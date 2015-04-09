using Coastguard.Data;
using Coastguard.Web.API.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Coastguard.Web.API.Controllers
{
    public class DonationController: BaseController
    {
        #region initialize
        private IOrganizationService _sdk;
        private Tracer _tracer = null;

        public DonationController(IOrganizationService sdk, Tracer tracer): base(sdk, tracer)
        {
            this._sdk = sdk;
            this._tracer = tracer;
        }
        #endregion

        #region donation process flow
        public void ProcessDonation(Donation donation)
        {
            this._tracer.Trace("Method: DonationController.ProcessDonation");

            Guid contactId = default(Guid);
            Guid pledgeId = default(Guid);
            Guid campaignTagId = default(Guid);
            Guid regionTagId = default(Guid);
            Guid transactionId = default(Guid);
            Guid donationId = default(Guid);
            Guid paymentItemId = default(Guid);
            MatchController matcher = new MatchController(this._tracer);

            // step 1: find a matching contact. create or update the contact based on the match
            contactId = MatchContact(donation, contactId, matcher);

            // get the campaign tag id of the related campaign (if any). we need to link this to both the pledge and donation
            campaignTagId = FindCampaign(donation, campaignTagId);

            // a region code will be passed from the website so we can set the coastguard unit
            regionTagId = FindRegion(donation, regionTagId);

            // step 2: match to a pledge. for one-off, match by contact, amount, is regular gift = no. for recurring, match by payment method, dps billing id, is regular gift = yes
            pledgeId = MatchPledge(donation, contactId, pledgeId, campaignTagId, regionTagId, matcher);

            // step 4: match to a payment batch. If we can't find one, we need to create a new batch
            Guid batchId = MatchPaymentBatch(donation, matcher);

            // handle rollbacks from this point forward. E.g. if a Donation fails to create, delete the related Payment Transaction
            Dictionary<Guid, string> recordsToDelete = new Dictionary<Guid, string>();

            try
            {
                // step 5: create a payment transaction - this is only for one-off payments
                if (!donation.IsRegularGift)
                {
                    transactionId = this.CreatePaymentTransaction(batchId, contactId, donation);
                    recordsToDelete.Add(transactionId, "mag_paymenttransaction");

                    // step 6: create a donation and link to the contact and pledge
                    donationId = this.CreateDonation(donation, contactId, pledgeId, regionTagId, campaignTagId);
                    recordsToDelete.Add(donationId, "mag_donation");

                    // step 7: create a payment item and link it to the donation and payment transaction
                    paymentItemId = this.CreatePaymentItem(transactionId, donationId, donation.Amount);
                    recordsToDelete.Add(paymentItemId, "mag_paymentitem");

                    // change the status reason of the Pledge to "Fulfilled"
                    this.SetPledgeStatus(pledgeId, 0, (int)PledgeStatusCode.Fulfilled);
                }
            }
            catch (Exception ex)
            {
                this._tracer.Trace(ex.ToString());
                Rollback(recordsToDelete);
                recordsToDelete.Clear();
                throw ex;
            }
        }

        private Guid MatchPaymentBatch(Donation donation, MatchController matcher)
        {
            List<Batch> paymentBatches = this.GetPaymentBatches(donation);
            Batch batch = matcher.MatchToExistingBatch(paymentBatches);

            Guid batchId = this.CreateOrRetrieveBatch(batch, donation);
            return batchId;
        }

        private Guid MatchPledge(Donation donation, Guid contactId, Guid pledgeId, Guid campaignTagId, Guid regionTagId, MatchController matcher)
        {
            if (contactId != default(Guid))
            {
                Pledge pledge = null;
                DateTime endDate = DateTime.Now;    // by default, set the end date to today. We'll use this for one-off billing
                PaymentRepeatOptionCode repeatOption = PaymentRepeatOptionCode.SetExpiryDate;

                this._tracer.Trace("donation.IsRegularGift={0}", donation.IsRegularGift);

                if (donation.IsRegularGift)
                {
                    List<Pledge> pledges = this.GetPledgesByBillingId(donation.Pledge.PledgeId);
                    pledge = matcher.MatchToExistingPledge(pledges, donation.Pledge.PledgeId);
                    if (donation.Pledge.EndDate.HasValue)
                    {
                        endDate = donation.Pledge.EndDate.Value;
                        this._tracer.Trace("donation.Pledge.EndDate={0}", donation.Pledge.EndDate.Value);
                    }
                    else
                    {
                        this._tracer.Trace("No end date. Payment Repeat Option=Until Further Notice");
                        repeatOption = PaymentRepeatOptionCode.UntilFurtherNotice;  // no end date
                    }
                }
                else
                {
                    List<Pledge> pledges = this.GetPledgesByContactAmount(contactId, donation.Amount);
                    pledge = matcher.MatchToExistingPledge(pledges, contactId, donation.Amount);
                }

                // step 3: create or link to an existing pledge
                pledgeId = this.CreateOrRetrievePledge(pledge, donation, contactId, regionTagId, campaignTagId, repeatOption);
            }
            return pledgeId;
        }

        private Guid FindRegion(Donation donation, Guid regionTagId)
        {
            regionTagId = GetRegionTagFromCampaign(donation.CampaignId);

            if (regionTagId == null || regionTagId == Guid.Empty)
            {
                if (!string.IsNullOrEmpty(donation.RegionCode))
                {
                    regionTagId = this.GetRegionTagId(donation.RegionCode);
                }
            }

            return regionTagId;
        }

        private Guid FindCampaign(Donation donation, Guid campaignTagId)
        {
            if (donation.CampaignId != default(Guid))
            {
                campaignTagId = this.GetCampaignTagId(donation.CampaignId);
            }
            return campaignTagId;
        }

        private Guid MatchContact(Donation donation, Guid contactId, MatchController matcher)
        {
            List<Contact> contacts = base.GetContactsByName(donation.Donor.FirstName, donation.Donor.LastName);
            Contact contactMatch = matcher.MatchToExistingContact(contacts, donation.Donor.MobilePhone, donation.Donor.HomePhone, donation.Donor.EmailAddress);

            // copy the values from the matched contact
            donation.Donor.ContactId = contactMatch.ContactId;
            donation.Donor.MatchCode = contactMatch.MatchCode;
            donation.Donor.IsDonor = true;

            contactId = base.CreateOrUpdateContact(donation.Donor, true);
            return contactId;
        }

        private void Rollback(Dictionary<Guid, string> recordsToDelete)
        {
            recordsToDelete.ToList().ForEach(r =>
            {
                try
                {
                    this._sdk.Delete(r.Value, r.Key);
                }
                catch { }   // if the deletion fails, not much else we can do. Let it silently fail. Duplicate records will need to be cleaned up manually.
            });
        }

        private Guid CreateOrRetrieveBatch(Batch batch, Donation d)
        {
            this._tracer.Trace("DonationController.CreateOrRetrieveBatch");

            Guid batchId = batch.BatchId;
            BatchMatchCode code = batch.MatchCode;

            if (code == BatchMatchCode.None)
            {
                this._tracer.Trace("MatchCode=None. Creating a new Payment Batch");
                batchId = this.CreatePaymentBatch(d);
            }

            return batchId;
        }

        private void SetPledgeStatus(Guid pledgeId, int stateCode, int statusCode)
        {
            this._tracer.Trace("Method: DonationController.SetPledgeStatus");
            this._tracer.Trace("Parameters: pledgeId={0}, stateCode={1}, statusCode={2}", pledgeId, stateCode, statusCode);

            this._sdk.Execute(new SetStateRequest
            {
                EntityMoniker = new EntityReference("mag_pledge", pledgeId),
                State = new OptionSetValue(stateCode),
                Status = new OptionSetValue(statusCode)
            });
        }

        private Guid CreateOrRetrievePledge(Pledge pledgeMatch, Donation donation, Guid contactId, Guid regionTagId, Guid campaignTagId, PaymentRepeatOptionCode repeatOption)
        {
            this._tracer.Trace("Method: DonationController.CreateOrRetrievePledge");
            this._tracer.Trace("Parameters: pledgeMatch, donation, contactId={0}, regionTagId={1}, campaignTagId={2}, repeatOption={3}", contactId, regionTagId, campaignTagId, repeatOption);

            Guid pledgeId = pledgeMatch.PledgeId;
            PledgeMatchCode code = pledgeMatch.MatchCode;

            if (code == PledgeMatchCode.None)
            {
                this._tracer.Trace("MatchCode=None. Creating a new Pledge");

                // create a new pledge
                pledgeId = this.CreatePledge(donation, contactId, regionTagId, campaignTagId, repeatOption);
            }

            return pledgeId;
        }

        #endregion

        #region crm create
        private Guid CreatePaymentItem(Guid transactionId, Guid donationId, decimal amount)
        {
            this._tracer.Trace("Method: DonationController.CreatePaymentItem");
            this._tracer.Trace("Parameters: transactionId={0}, donationId={1}, amount={2}", transactionId, donationId, amount);

            Entity paymentItem = new Entity("mag_paymentitem");
            paymentItem["mag_transactionid"] = MappingController.MapToEntityReference("mag_paymenttransaction", transactionId);
            paymentItem["mag_donationid"] = MappingController.MapToEntityReference("mag_donation", donationId);
            paymentItem["mag_amount"] = new Money(amount);

            return this._sdk.Create(paymentItem);
        }

        private Guid CreatePaymentTransaction(Guid batchId, Guid contactId, Donation donation)
        {
            this._tracer.Trace("Method: DonationController.CreatePaymentTransaction");
            this._tracer.Trace("Parameters: batchId={0}, contactId={1}, donation", batchId, contactId);

            Entity paymentTransaction = new Entity("mag_paymenttransaction");
            paymentTransaction["mag_batchid"] = MappingController.MapToEntityReference("mag_paymentbatch", batchId);
            paymentTransaction["mag_contactid"] = MappingController.MapToEntityReference("contact", contactId);
            paymentTransaction["mag_amount"] = new Money(donation.Amount);
            paymentTransaction["mag_isprocessed"] = true;
            paymentTransaction["mag_transactiondate"] = donation.Date;
            paymentTransaction["mag_transactionid"] = donation.DpsTransactionReference;
            paymentTransaction["mag_transactiontypecode"] = new OptionSetValue((int)PaymentTransactionTypeCode.CreditCard);

            return this._sdk.Create(paymentTransaction);
        }

        private Guid CreatePaymentBatch(Donation d)
        {
            this._tracer.Trace("Method: CreatePaymentBatch");

            Entity paymentBatch = new Entity("mag_paymentbatch");
            paymentBatch["mag_batchtypecode"] = new OptionSetValue((int)BatchTypeCode.CreditCard);
            paymentBatch["mag_transactiondate"] = d.Date;
            paymentBatch["mag_batchstatuscode"] = new OptionSetValue((int)BatchStatusCode.Open);
            paymentBatch["mag_importsource"] = "Credit Card Batch Processing";

            return this._sdk.Create(paymentBatch);
        }

        public Guid CreateDonation(Donation d, Guid contactId, Guid pledgeId, Guid regionTagId, Guid campaignTagId)
        {
            this._tracer.Trace("Method: DonationController.CreateDonation");
            this._tracer.Trace("Parameters: d, contactId={0}, pledgeId={1}, regionTagId={2}, campaignTagId={3}", contactId, pledgeId, regionTagId, campaignTagId);

            Entity donation = new Entity("mag_donation");
            donation["mag_donationforcode"] = new OptionSetValue((int)DonationForCode.Individual);
            donation["mag_contactid"] = MappingController.MapToEntityReference("contact", contactId);
            donation["statuscode"] = new OptionSetValue((int)DonationStatusCode.Collected);
            donation["mag_pledgeid"] = MappingController.MapToEntityReference("mag_pledge", pledgeId);
            donation["mag_campaignid"] = MappingController.MapToEntityReference("campaign", d.CampaignId);
            donation["mag_campaigntagid"] = MappingController.MapToEntityReference("mag_tag", campaignTagId);
            donation["mag_coastguardregionunittagid"] = MappingController.MapToEntityReference("mag_tag", regionTagId);
            donation["mag_donationtypecode"] = new OptionSetValue((int)DonationTypeCode.Cash);
            donation["mag_donationdate"] = d.Date;
            donation["mag_donationcloseddate"] = d.Date;
            donation["mag_cashvalue"] = new Money(d.Amount);
            donation["mag_isregulargift"] = d.IsRegularGift;
            donation["mag_paymentmethodcode"] = new OptionSetValue((int)PaymentMethodCode.CreditCard);
            donation["mag_transactionid"] = d.DpsTransactionReference;
            donation["mag_transactionstatus"] = d.DpsResponseText;
            donation["mag_importsource"] = "Online Donation";
            donation["mag_taxreceiptname"] = string.IsNullOrEmpty(d.Donor.NameOnTaxReceipt) ? string.Format("{0} {1}", d.Donor.FirstName, d.Donor.LastName) : d.Donor.NameOnTaxReceipt;
            donation["mag_comments"] = d.Comments;
            donation["mag_reasonforhelpingid"] = MappingController.MapToEntityReference("mag_reasonforhelping", d.ReasonForHelpingId);
            donation["mag_receipted"] = true;
            donation["mag_receiptdate"] = DateTime.Now;

            // set the owner to the owner of the contact
            if (d.Donor.OwnerId != default(Guid))
            {
                this._tracer.Trace("Donation ownerid={0}, ownerType={1}", d.Donor.OwnerId, d.Donor.OwnerLogicalName);
                donation["ownerid"] = MappingController.MapToEntityReference(d.Donor.OwnerLogicalName, d.Donor.OwnerId);
            }

            return this._sdk.Create(donation);
        }

        public Guid CreatePledge(Donation donation, Guid contactId, Guid regionTagId, Guid campaignTagId, PaymentRepeatOptionCode repeatOption)
        {
            this._tracer.Trace("Method: DonationController.CreatePledge");
            this._tracer.Trace("Parameters: donation, contactId={0}, regionTagId={1}, campaignTagId={2}, repeatOption={3}", contactId, regionTagId, campaignTagId, repeatOption);

            Entity pledge = new Entity("mag_pledge");
            pledge["mag_dpsbillingid"] = donation.Pledge.DpsBillingId;
            pledge["mag_pledgeforcode"] = new OptionSetValue((int)DonationForCode.Individual);
            pledge["mag_contactid"] = MappingController.MapToEntityReference("contact", contactId);
            pledge["mag_campaignid"] = MappingController.MapToEntityReference("campaign", donation.CampaignId);
            pledge["mag_campaigntagid"] = MappingController.MapToEntityReference("mag_tag", campaignTagId);
            pledge["mag_coastguardregionunittagid"] = MappingController.MapToEntityReference("mag_tag", regionTagId);
            pledge["mag_pledgetypecode"] = new OptionSetValue((int)DonationTypeCode.Cash);
            pledge["mag_isregulargift"] = donation.IsRegularGift;
            pledge["mag_paymentfrequencycode"] = MappingController.MapToOptionSetValue(donation.Pledge.PaymentFrequencyCode);
            pledge["mag_comments"] = donation.Comments;
            pledge["mag_reasonforhelpingid"] = MappingController.MapToEntityReference("mag_reasonforhelping", donation.ReasonForHelpingId);

            // set the start date to the pledge start date, or use today's date. This is for the naming plugin
            pledge["mag_startdate"] = donation.Pledge.StartDate.HasValue ? GetUtcDateTime(donation.Pledge.StartDate.Value) : DateTime.UtcNow;    

            pledge["mag_paymentmethodcode"] = new OptionSetValue((int)PaymentMethodCode.CreditCard);
            pledge["mag_cashvalue"] = new Money(donation.Amount);
            pledge["mag_importsource"] = "Online Donation";
            pledge["mag_transactionid"] = donation.DpsTransactionReference;

            // set the payment repeat option and end date for regular gifts
            if (donation.IsRegularGift)
            {
                pledge["mag_paymentrepeatoptioncode"] = new OptionSetValue((int)repeatOption);
                pledge["mag_enddate"] = donation.Pledge.EndDate;
                pledge["mag_creditcardexpirydate"] = base.GetCCExpiryDate(donation.CCExpiry);
            }

            // set the owner to the owner of the contact
            if (donation.Donor.OwnerId != default(Guid))
            {
                this._tracer.Trace("Pledge ownerid={0}, ownerType={1}", donation.Donor.OwnerId, donation.Donor.OwnerLogicalName);
                pledge["ownerid"] = MappingController.MapToEntityReference(donation.Donor.OwnerLogicalName, donation.Donor.OwnerId);
            }

            return this._sdk.Create(pledge);
        }

        private DateTime GetUtcDateTime(DateTime dt)
        {
            // website says they're sending UTC but actually sending local time...fuckers!!!
            return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 1, DateTimeKind.Local).ToUniversalTime();
        }
        #endregion

        #region crm retrieve
        private List<Batch> GetPaymentBatches(Donation d)
        {
            this._tracer.Trace("Method: DonationController.GetPaymentBatches");

            DateTime transactionDate = d.Date;
            this._tracer.Trace("transactionDate={0}", d.Date);

            QueryExpression query = new QueryExpression { EntityName = "mag_paymentbatch" };
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);   // active
            query.Criteria.AddCondition("mag_batchstatuscode", ConditionOperator.Equal, (int)BatchStatusCode.Open);
            query.Criteria.AddCondition("mag_batchtypecode", ConditionOperator.Equal, (int)BatchTypeCode.CreditCard);
            query.Criteria.AddCondition("mag_transactiondate", ConditionOperator.On, transactionDate);
            query.Criteria.AddCondition("mag_importsource", ConditionOperator.Equal, "Credit Card Batch Processing");

            this._tracer.Trace("Retrieving Payment Batches");
            EntityCollection results = this._sdk.RetrieveMultiple(query);
            this._tracer.Trace("Number of Payment Batches Found={0}", results.Entities.Count);

            List<Batch> batches = (from e in results.Entities.ToList()
                                   select new Batch
                                   {
                                       BatchId = e.Id
                                   }).ToList();

            return batches;
        }

        public List<Region> GetRegions()
        {
            List<Region> results = new List<Region>();

            QueryExpression qe = new QueryExpression("mag_tag") { ColumnSet = new ColumnSet("mag_name", "mag_tagid") };
            qe.Criteria.AddCondition("mag_appliestocode", ConditionOperator.Equal, 809730001);
            qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            var r = this._sdk.RetrieveMultiple(qe);
            if (r != null && r.Entities != null && r.Entities.Count > 0)
            {
                results = r.Entities.Select(a =>
                    {
                        return new Region { Id = a.Get<Guid>("mag_tagid"), Name = a.Get<string>("mag_name") };
                    }).ToList();
            }

            return results;
        }

        public List<Campaign> GetCampaigns(string regionCode)
        {
            this._tracer.Trace("Method: GetCampaigns");
            this._tracer.Trace("Parameters: regionCode={0}", regionCode);

            List<Campaign> campaigns = null;

            try
            {
                QueryExpression query = new QueryExpression { EntityName = "campaign", ColumnSet = new ColumnSet("name") };
                query.Criteria.AddCondition("mag_publishtoweb", ConditionOperator.Equal, true);
                query.AddOrder("name", OrderType.Ascending);

                // link to business unit so we can check the region code
                LinkEntity link = new LinkEntity
                {
                    LinkFromEntityName = "campaign",
                    LinkToEntityName = "businessunit",
                    LinkFromAttributeName = "owningbusinessunit",
                    LinkToAttributeName = "businessunitid",
                };

                link.LinkCriteria.AddCondition("mag_regioncode", ConditionOperator.Equal, regionCode);
                query.LinkEntities.Add(link);

                this._tracer.Trace("Getting Campaigns");
                EntityCollection results = this._sdk.RetrieveMultiple(query);
                this._tracer.Trace("Number of Campaigns found: {0}", results.Entities.Count);

                campaigns = (from e in results.Entities.ToList()
                             select new Campaign
                             {
                                 CampaignId = e.Id,
                                 Name = e.Get<string>("name")
                             }).ToList();
            }
            catch (Exception ex)
            {
                this._tracer.Trace("Unable to retrieve Campaigns from CRM");
                this._tracer.Trace(ex.ToString());
            }

            return campaigns;
        }

        public List<ReasonForHelping> GetReasonsForHelping()
        {
            QueryExpression query = new QueryExpression { EntityName = "mag_reasonforhelping" };
            query.AddOrder("mag_name", OrderType.Ascending);

            EntityCollection results = this._sdk.RetrieveMultiple(query);

            List<ReasonForHelping> reasons = (from e in results.Entities.ToList()
                                              orderby e.Get<string>("mag_name")
                                              select new ReasonForHelping
                                              {
                                                  ReasonForHelpingId = e.Id,
                                                  Name = e.Get<string>("mag_name")
                                              }).ToList();

            return reasons;
        }

        private Guid GetRegionTagFromCampaign(Guid campaignId)
        {
            this._tracer.Trace("GetRegionTagFromCampaign: campaignId={0}", campaignId);

            Guid result = Guid.Empty;

            if (campaignId != null && campaignId != Guid.Empty)
            {
                var entity = this._sdk.Retrieve("campaign", campaignId, new ColumnSet("mag_tagid"));
                if (entity != null)
                {
                    result = entity.Get<Guid>("mag_tagid");
                }
            }

            this._tracer.Trace("GetRegionTagFromCampaign: result={0}", result);
            return result;
        }

        public Guid GetCampaignTagId(Guid campaignId)
        {
            this._tracer.Trace("Method: DonationController.GetCampaignTagId");
            this._tracer.Trace("Parameters: campaignId={0}", campaignId);

            Guid campaignTagId = default(Guid);

            QueryExpression query = new QueryExpression { EntityName = "mag_tag" }; // no columnset. just want the id
            query.Criteria.AddCondition("mag_campaignid", ConditionOperator.Equal, campaignId);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);   // active 0

            this._tracer.Trace("Retrieving Campaign Tag");
            EntityCollection results = this._sdk.RetrieveMultiple(query);
            this._tracer.Trace("Number of Campaign Tags found={0}", results.Entities.Count);

            if (results.Entities.Count > 0)
            {
                campaignTagId = results.Entities[0].Id;
            }

            this._tracer.Trace("campaignTagId={0}", campaignTagId);

            return campaignTagId;
        }

        public Guid GetRegionTagId(string regionCode)
        {
            this._tracer.Trace("Method: DonationController.GetRegionTagId");
            this._tracer.Trace("Parameters: regionCode={0}", regionCode);

            Guid regionTagId = default(Guid);

            QueryExpression query = new QueryExpression { EntityName = "mag_tag" }; // no column set. just want the id
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);   // active only

            LinkEntity link = new LinkEntity
            {
                LinkFromEntityName = "mag_tag",
                LinkToEntityName = "businessunit",
                LinkFromAttributeName = "mag_coastguardregionid",
                LinkToAttributeName = "businessunitid"
            };

            link.LinkCriteria.AddCondition("mag_regioncode", ConditionOperator.Equal, regionCode);
            query.LinkEntities.Add(link);

            this._tracer.Trace("Retrieving Region Tag");
            EntityCollection results = this._sdk.RetrieveMultiple(query);
            this._tracer.Trace("Number of Region Tags found={0}", results.Entities.Count);

            if (results.Entities.Count > 0)
            {
                regionTagId = results.Entities[0].Id;
            }

            this._tracer.Trace("regionTagId={0}", regionTagId);

            return regionTagId;
        }

        public List<Pledge> GetPledgesByBillingId(Guid dpsBillingId)
        {
            this._tracer.Trace("Method: DonationController.GetPledgesByBillingId");
            this._tracer.Trace("Parameters: dpsBillingId={0}", dpsBillingId);

            QueryExpression query = new QueryExpression { EntityName = "mag_pledge" };
            query.Criteria.AddCondition("mag_dpsbillingid", ConditionOperator.Equal, dpsBillingId.ToString());
            query.Criteria.AddCondition("mag_paymentmethodcode", ConditionOperator.Equal, (int)PaymentMethodCode.CreditCard);
            query.Criteria.AddCondition("mag_isregulargift", ConditionOperator.Equal, true);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);   // active
            query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, 1);  // active

            this._tracer.Trace("Retrieving Pledges");
            EntityCollection results = this._sdk.RetrieveMultiple(query);
            this._tracer.Trace("Number of Pledges found={0}", results.Entities.Count);

            List<Pledge> pledges = (from e in results.Entities.ToList()
                                    select new Pledge
                                    {
                                        PledgeId = e.Id
                                    }).ToList();

            return pledges;
        }

        public List<Pledge> GetPledgesByContactAmount(Guid contactId, decimal amount)
        {
            this._tracer.Trace("Method: DonationController.GetPledgesByContactAmount");
            this._tracer.Trace("Parameters: contactId={0}, amount={1}", contactId, amount);

            QueryExpression query = new QueryExpression { EntityName = "mag_pledge" };
            query.Criteria.AddCondition("mag_contactid", ConditionOperator.Equal, contactId);
            query.Criteria.AddCondition("mag_cashvalue", ConditionOperator.Equal, amount);
            query.Criteria.AddCondition("mag_isregulargift", ConditionOperator.Equal, false);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);   // active
            query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, 1);  // active

            this._tracer.Trace("Retrieving Pledges");
            EntityCollection results = this._sdk.RetrieveMultiple(query);
            this._tracer.Trace("Number of Pledges found={0}", results.Entities.Count);

            List<Pledge> pledges = (from e in results.Entities.ToList()
                                    select new Pledge
                                    {
                                        PledgeId = e.Id
                                    }).ToList();

            return pledges;
        }
        #endregion
    }
}
