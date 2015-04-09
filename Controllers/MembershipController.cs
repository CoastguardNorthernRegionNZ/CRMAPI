using Coastguard.Data;
using Coastguard.Web.API.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Coastguard.Web.API.Controllers
{
    public class MembershipController : BaseController
    {
        #region initialization
        private IOrganizationService _sdk;
        private Tracer _tracer = null;

        public MembershipController(IOrganizationService sdk, Tracer tracer)
            : base(sdk, tracer)
        {
            this._sdk = sdk;
            this._tracer = tracer;
        }
        #endregion

        #region membership process flow
        /// <summary>
        /// Creates a new Membership in CRM. Matches against existing Contacts in CRM. If no Contact is found, a new one is created and is linked to the new Membership
        /// </summary>
        public void ProcessCreate(Membership membership)
        {
            this._tracer.Trace("Method: MembershipController.ProcessCreate");

            MatchController mc = new MatchController(this._tracer);

            // step 1: find a matching contact in CRM
            membership.Member = MatchContact(membership, mc);

            // step 2: coastguard code will be passed in representing the unit. We need to get the actual Coastguard Unit GUID from CRM
            Guid coastguardUnitCodeId = membership.UnitCodeId;
            Guid unitId = this.GetUnitId(coastguardUnitCodeId);

            // step 2: create a new membership in CRM
            this.CreateMembership(membership, unitId);
        }

        /// <summary>
        /// Gets the GUID of a Coastguard Unit based on the GUID of its corresponding Coastguard Code
        /// </summary>ns>
        private Guid GetUnitId(Guid coastguardUnitCodeId)
        {
            this._tracer.Trace("Method: MembershipController.GetUnitId");
            this._tracer.Trace("Parameters: coastguardUnitCodeId={0}", coastguardUnitCodeId);

            Guid unitId = default(Guid);

            QueryExpression query = new QueryExpression { EntityName = "mag_tag", ColumnSet = new ColumnSet("mag_coastguardunitid") };
            query.Criteria.AddCondition("mag_tagid", ConditionOperator.Equal, coastguardUnitCodeId);

            EntityCollection results = this._sdk.RetrieveMultiple(query);
            if (results.Entities.Count > 0)
            {
                unitId = results.Entities[0].Get<Guid>("mag_coastguardunitid");
            }

            this._tracer.Trace("unitId={0}", unitId);

            return unitId;
        }

        public bool RenewMembership(MembershipRenewal renewal)
        {
            bool renewed = false;

            // Look for an Active Invoice, with a Pending Renewal Membership attached
            Entity membership = this.GetMembershipAndInvoice(renewal.MembershipNumber);
            if (membership == null)
            {
                Guid membershipId = this.GetLatestActiveMembershipId(renewal.MembershipNumber, true);
                if (membershipId != null && membershipId != Guid.Empty)
                {
                    // Tag the active membership for renewing
                    this.TriggerRenewal(membershipId);

                    // Plugin runs on change of 'mag_pendingrenewalcomplete' which triggers a sync plugin to create the renewal membership and invoice
                    // --

                    // Invoice should now be created, so this will now give us the new invoice/membership
                    membership = this.GetMembershipAndInvoice(renewal.MembershipNumber);
                }
                else
                {
                    _tracer.Trace("membershipNumber={0} is not valid for renewal.", renewal.MembershipNumber);
                }
            }

            if (membership != null)
            {
                this._tracer.Trace("membershipId={0}", membership.Id);

                this.UpdateMembershipAndInvoice(membership, renewal.MembershipType.MembershipTypeId, renewal.MembershipType.Price);

                renewed = true;
            }

            return renewed;
        }

        // This method has been replaced with the one above, to capture the membership type as well as the price
        public bool RenewMembership(Renewal renewal)
        {
            bool renewed = false;

            Guid invoiceId = GetRelatedUnpaidInvoice(renewal.MembershipNumber);
            this._tracer.Trace("invoiceId={0}", invoiceId);
            if (invoiceId != default(Guid))
            {
                this.UpdateInvoicePaid(invoiceId);
                renewed = true;

                // Get the contactId for the payment item (this was a yuck last minute change)
                Entity inv = this._sdk.Retrieve("invoice", invoiceId, new ColumnSet("customerid"));

                // Create a payment item for the invoice
                this.ProcessPaymentItem(invoiceId, inv.Get<Guid>("customerid"), renewal.Amount);
            }
            else
            {
                Guid membershipId = this.GetLatestActiveMembershipId(renewal.MembershipNumber, true);
                if (membershipId != null && membershipId != Guid.Empty)
                {
                    this.TriggerRenewal(membershipId);
                    renewed = true;

                    // Plugin runs on change of 'mag_pendingrenewalcomplete' which triggers a sync plugin to create the renewal membership and invoice

                    this.UpdateRenewedMembership(membershipId, renewal.Amount);
                }
            }

            return renewed;
        }

        private void UpdateMembershipAndInvoice(Entity membership, Guid membershipTypeId, decimal amountPaid)
        {
            Guid invoiceId = membership.Get<Guid>("mag_invoiceid");
            Guid existingMembershipTypeId = membership.Get<Guid>("mag_membershiptypeid");
            Guid contactId = membership.Get<Guid>("mag_contactid");

            // Member can change membership type online, if they do that we need to change EVERYTHING!!
            if (existingMembershipTypeId != membershipTypeId)
            {
                // Change the membership type, amount, end date, and payment method for the membership and related invoice
                this.ChangeMembershipType(membership.Id, membershipTypeId, amountPaid, invoiceId);
            }
            else
            {
                // If the membership type hasnt changed, still need to set the payment method, otherwise we set this in the same update as changing the type etc
                this.SetPaymentMethod(membership.Id, (int)PaymentMethodCode.CreditCard);
            }

            this.UpdateInvoicePaid(invoiceId);

            // Create a payment item for the invoice
            this.ProcessPaymentItem(invoiceId, contactId, amountPaid);
        }

        private void ChangeMembershipType(Guid membershipId, Guid membershipTypeId, decimal amountPaid, Guid invoiceId)
        {
            _tracer.Trace("ChangeMembershipType, membershipTypeId={0}", membershipTypeId);

            // Update the Membership Type, Amount, End Date, and Payment Method
            Entity membership = new Entity("mag_membership");
            membership["mag_membershipid"] = membershipId;
            membership["mag_paymentmethodcode"] = new OptionSetValue((int)PaymentMethodCode.CreditCard);
            membership["mag_subscriptionfee"] = new Money(amountPaid);
            membership["mag_discountamount"] = new Money(0);
            membership["mag_actualfee"] = new Money(amountPaid);
            membership["mag_membershiptypeid"] = new EntityReference("product", membershipTypeId);

            this._sdk.Update(membership);
            _tracer.Trace("updated membership type for membershipId={0}", membershipId);

            // Update the Invoice Product and Amount
            Entity product = this._sdk.Retrieve("product", membershipTypeId, new ColumnSet("defaultuomid"));
            EntityReference unit = product.Get<EntityReference>("defaultuomid");

            // Get the tax percentage from settings
            decimal taxPercentage = this.GetTaxPercentage();
            decimal pricePerUnit = amountPaid / (1 + (taxPercentage / 100));

            // Get the ID of the invoice product (memberships only have 1)
            Guid invoiceProductId = GetInvoiceProductId(invoiceId);
            if (invoiceProductId != default(Guid))
            {
                Entity invProd = new Entity("invoicedetail");
                invProd["invoicedetailid"] = invoiceProductId;
                invProd["productid"] = new EntityReference("product", membershipTypeId);
                invProd["uomid"] = unit;
                invProd["priceperunit"] = new Money(pricePerUnit);
                //invProd["tax"] = new Money(amountPaid - pricePerUnit); // System will calculate this instead

                this._sdk.Update(invProd);
                _tracer.Trace("updated membership type for invoiceProductId={0}", invoiceProductId);
            }
        }

        private Guid GetInvoiceProductId(Guid invoiceId)
        {
            Guid invoiceProductId = Guid.Empty;

            QueryExpression qe = new QueryExpression("invoicedetail");
            qe.NoLock = true;
            qe.Criteria.AddCondition("invoiceid", ConditionOperator.Equal, invoiceId);

            EntityCollection results = this._sdk.RetrieveMultiple(qe);
            if (results != null && results.Entities != null && results.Entities.Count > 0)
            {
                invoiceProductId = results.Entities[0].Id;
            }

            return invoiceProductId;
        }

        private decimal GetTaxPercentage()
        {
            decimal tax = 0;

            QueryExpression qe = new QueryExpression("mag_setting") { ColumnSet = new ColumnSet("mag_value") };
            qe.NoLock = true;
            qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            qe.Criteria.AddCondition("mag_key", ConditionOperator.Equal, "Tax.Percentage");

            var collection = this._sdk.RetrieveMultiple(qe);
            if (collection != null && collection.Entities.Count > 0)
            {
                string value = collection.Entities[0].Get<string>("mag_value");
                decimal.TryParse(value, out tax);
            }

            return tax;
        }

        // This method has been deprecated and replaced with UpdateMembershipAndInvoice
        private void UpdateRenewedMembership(Guid membershipId, decimal amount)
        {
            QueryExpression qe = new QueryExpression("mag_membership") { ColumnSet = new ColumnSet("mag_invoiceid", "mag_contactid") };
            qe.Criteria.AddCondition("mag_renewedfromid", ConditionOperator.Equal, membershipId);
            qe.Criteria.AddCondition("statuscode", ConditionOperator.Equal, 809730000); // Pending Renewal

            EntityCollection results = this._sdk.RetrieveMultiple(qe);
            if (results != null && results.Entities != null && results.Entities.Count > 0)
            {
                Entity membership = results[0];

                Guid invoiceId = membership.Get<Guid>("mag_invoiceid");
                if (invoiceId != null && invoiceId != Guid.Empty)
                {
                    this.SetPaymentMethod(membership.Id, (int)PaymentMethodCode.CreditCard);

                    this.UpdateInvoicePaid(invoiceId);

                    // Create a payment item for the invoice
                    this.ProcessPaymentItem(invoiceId, membership.Get<Guid>("mag_contactid"), amount);
                }
            }
        }

        /// <summary>
        /// Creates a payment item and transaction for the paid invoice, against the 'Website' payment batch for today
        /// </summary>
        private void ProcessPaymentItem(Guid invoiceId, Guid contactId, decimal amount)
        {
            Guid paymentBatchId = this.GetWebsitePaymentBatch();

            // Create transaction
            Entity trans = new Entity("mag_paymenttransaction");
            trans["mag_contactid"] = MappingController.MapToEntityReference("contact", contactId);
            trans["mag_batchid"] = MappingController.MapToEntityReference("mag_paymentbatch", paymentBatchId);
            trans["mag_isprocessed"] = true;
            trans["mag_transactiontypecode"] = new OptionSetValue(809730002); // Credit Card
            trans["mag_transactiondate"] = DateTime.Today;
            trans["mag_amount"] = new Money(amount);

            Guid transId = this._sdk.Create(trans);

            // Create payment item
            Entity item = new Entity("mag_paymentitem");
            item["mag_transactionid"] = MappingController.MapToEntityReference("mag_paymenttransaction", transId);
            item["mag_invoiceid"] = MappingController.MapToEntityReference("invoice", invoiceId);
            item["mag_amount"] = new Money(amount);

            Guid paymentItemId = this._sdk.Create(item);
            _tracer.Trace("paymentItemId={0}", paymentItemId);
        }

        private Guid GetWebsitePaymentBatch()
        {
            Guid id = Guid.Empty;

            QueryExpression qe = new QueryExpression("mag_paymentbatch");
            qe.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0); // Active
            qe.Criteria.AddCondition("mag_batchtypecode", ConditionOperator.Equal, 809730001); // Credit Card
            qe.Criteria.AddCondition("mag_transactiondate", ConditionOperator.On, DateTime.Today);
            qe.Criteria.AddCondition("mag_importsource", ConditionOperator.Equal, "Website");
            qe.Criteria.AddCondition("mag_batchstatuscode", ConditionOperator.Equal, 809730000); // Open
            qe.Criteria.AddCondition("mag_paymentfrequencycode", ConditionOperator.Equal, 809730005); // One-Off

            EntityCollection results = this._sdk.RetrieveMultiple(qe);
            if (results != null && results.Entities.Count > 0)
            {
                // Get the first match (should only be one)
                id = results.Entities[0].Id;
            }
            else
            {
                // Create todays payment batch
                Entity batch = new Entity("mag_paymentbatch");
                batch["mag_batchtypecode"] = new OptionSetValue(809730001); // Credit Card
                batch["mag_transactiondate"] = DateTime.Today;
                batch["mag_importsource"] = "Website";
                batch["mag_batchstatuscode"] = new OptionSetValue(809730000); // Open
                batch["mag_paymentfrequencycode"] = new OptionSetValue(809730005); // One-Off

                id = this._sdk.Create(batch);
            }

            return id;
        }

        private void SetPaymentMethod(Guid membershipId, int option)
        {
            Entity renewal = new Entity("mag_membership");
            renewal["mag_membershipid"] = membershipId;
            renewal["mag_paymentmethodcode"] = new OptionSetValue(option);

            this._sdk.Update(renewal);
        }

        private void TriggerRenewal(Guid membershipId)
        {
            Entity m = new Entity("mag_membership");
            m["mag_membershipid"] = membershipId;
            m["mag_pendingrenewalcomplete"] = true;
            //m["mag_markinvoiceaspaid"] = true;
            //m["mag_membershipsourcecode"] = new OptionSetValue(809730000); // online

            this._sdk.Update(m);
        }

        private void UpdateInvoicePaid(Guid invoiceId)
        {
            this._sdk.Execute(new SetStateRequest
            {
                EntityMoniker = new EntityReference("invoice", invoiceId),
                State = new OptionSetValue((int)InvoiceStateCode.Paid),
                Status = new OptionSetValue((int)InvoiceStatusCode.Complete)
            });
        }

        /// <summary>
        /// Retrieves a Membership based on the membership number and looks for a related Invoice where the Status is not "Paid"
        /// </summary>
        private Entity GetMembershipAndInvoice(string membershipNumber)
        {
            Entity membership = null;

            QueryExpression query = new QueryExpression { EntityName = "mag_membership" };
            query.ColumnSet = new ColumnSet("mag_membershiptypeid", "mag_invoiceid", "mag_contactid");
            query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, (int)MembershipStatusCode.PendingRenewal);    // pending renewal
            query.Criteria.AddCondition("mag_idnumber", ConditionOperator.Equal, membershipNumber);
            query.Criteria.AddCondition("mag_paymentreceived", ConditionOperator.Null);

            query.AddOrder("mag_startdate", OrderType.Descending);  // we want the most recent membership

            // check for a linked invoice that is pending renewal
            LinkEntity invoiceLink = new LinkEntity
            {
                LinkFromEntityName = "mag_membership",
                LinkToEntityName = "invoice",
                LinkFromAttributeName = "mag_invoiceid",
                LinkToAttributeName = "invoiceid",
                EntityAlias = "invoice"
            };

            //invoice.LinkCriteria.AddCondition("statecode", ConditionOperator.NotEqual, (int)InvoiceStateCode.Paid);
            invoiceLink.LinkCriteria.AddCondition("statecode", ConditionOperator.Equal, (int)InvoiceStateCode.Active); // We don't want Cancelled Invoices - PN - 27/11/13
            query.LinkEntities.Add(invoiceLink);

            EntityCollection results = this._sdk.RetrieveMultiple(query);
            if (results.Entities.Count > 0)
            {
                //invoiceId = (Guid)results.Entities[0].Get<AliasedValue>("invoice.invoiceid").Value;
                membership = results.Entities[0];
            }

            return membership;
        }

        /// <summary>
        /// NOTE: Replaced with method above to handle returning membership fields as well
        /// 
        /// Retrieves a Membership based on the membership number and looks for a related Invoice where the Status is not "Paid"
        /// </summary>
        private Guid GetRelatedUnpaidInvoice(string membershipNumber)
        {
            Guid invoiceId = default(Guid);

            QueryExpression query = new QueryExpression { EntityName = "mag_membership" };
            query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, (int)MembershipStatusCode.PendingRenewal);    // pending renewal
            query.Criteria.AddCondition("mag_idnumber", ConditionOperator.Equal, membershipNumber);

            query.AddOrder("createdon", OrderType.Descending);  // we want the most recent membership

            // check for a linked invoice that is pending renewal
            LinkEntity invoice = new LinkEntity
            {
                LinkFromEntityName = "mag_membership",
                LinkToEntityName = "invoice",
                LinkFromAttributeName = "mag_invoiceid",
                LinkToAttributeName = "invoiceid",
                EntityAlias = "invoice",
                Columns = new ColumnSet("invoiceid")
            };

            //invoice.LinkCriteria.AddCondition("statecode", ConditionOperator.NotEqual, (int)InvoiceStateCode.Paid);
            invoice.LinkCriteria.AddCondition("statecode", ConditionOperator.Equal, (int)InvoiceStateCode.Active); // We don't want Cancelled Invoices - PN - 27/11/13
            query.LinkEntities.Add(invoice);

            EntityCollection results = this._sdk.RetrieveMultiple(query);
            if (results.Entities.Count > 0)
            {
                invoiceId = (Guid)results.Entities[0].Get<AliasedValue>("invoice.invoiceid").Value;
            }

            return invoiceId;
        }

        /// <summary>
        /// Updates the Contact and/or Membership details for an existing Membership
        /// </summary>
        public void ProcessUpdate(Membership membership)
        {
            Contact member = membership.Member;
            member.MatchCode = ContactMatchCode.Email;

            Guid contactId = base.CreateOrUpdateContact(member, false);

            // NOTE: There is no web page currently available for Members to update their Membership so this method is not required.
        }

        /// <summary>
        /// Find a matching Membership in CRM based on the contact id, membership number, and associated "unpaid" invoice
        /// NOTE: UNUSED METHOD
        /// </summary>
        //private Membership MatchMembership(Membership membership, MatchController mc)
        //{
        //    Guid contactId = membership.Member.ContactId;
        //    string membershipNumber = membership.MembershipNumber;

        //    List<Membership> memberships = this.GetMemberships(contactId, membershipNumber);
        //    Membership match = mc.MatchToExistingMembership(memberships);

        //    membership.MatchCode = match.MatchCode; // set the match code

        //    // create a membership
        //    if (membership.MembershipId == default(Guid))
        //    {
        //        membership.MembershipId = this.CreateMembership(membership);
        //    }

        //    return membership;
        //}

        /// <summary>
        /// Creates a new Membership in CRM for the associated Contact. Note - an Invoice will automatically be created from a plugin
        /// </summary>
        private Guid CreateMembership(Membership membership, Guid unitId)
        {
            this._tracer.Trace("Method: MembershipController.CreateMembership");
            this._tracer.Trace("Parameters: unitId={0}", unitId);

            Entity mShip = new Entity("mag_membership");
            mShip["mag_idnumber"] = membership.MembershipNumber;
            mShip["mag_contactid"] = MappingController.MapToEntityReference("contact", membership.Member.ContactId);
            mShip["mag_membershiptypeid"] = MappingController.MapToEntityReference("product", membership.MembershipType.MembershipTypeId);
            mShip["mag_startdate"] = DateTime.Now;
            mShip["mag_regionid"] = MappingController.MapToEntityReference("businessunit", membership.RegionCodeId);
            mShip["mag_unitid"] = MappingController.MapToEntityReference("mag_coastguardunit", unitId);
            mShip["mag_transactionid"] = membership.DpsTransactionReference;
            mShip["mag_membershipsourcecode"] = new OptionSetValue((int)MembershipSourceCode.Online); // online
            mShip["mag_paymentmethodcode"] = new OptionSetValue((int)PaymentMethodCode.CreditCard);
            mShip["mag_creditcardexpirydate"] = base.GetCCExpiryDate(membership.CCExpiry);
            mShip["mag_membershipsupportsid"] = MappingController.MapToEntityReference("mag_tag", membership.UnitCodeId);

            Guid membershipId = this._sdk.Create(mShip);

            // Plugin triggers to generate and link the invoice with the new membership

            // Get the new membership with the invoice ID and pay the invoice
            Entity newMembership = this._sdk.Retrieve("mag_membership", membershipId, new ColumnSet("mag_invoiceid"));
            Guid invoiceId = newMembership.Get<Guid>("mag_invoiceid");
            if (invoiceId != null && invoiceId != Guid.Empty)
            {
                this.UpdateInvoicePaid(invoiceId);

                // Create a payment item for the invoice
                this.ProcessPaymentItem(invoiceId, membership.Member.ContactId, membership.MembershipType.Price);
            }

            return membershipId;
        }

        /// <summary>
        /// Find a matching Contact in CRM based on the first name, last name, home phone, mobile phone, email address
        /// </summary>
        private Contact MatchContact(Membership membership, MatchController mc)
        {
            this._tracer.Trace("Method: MembershipController.MatchContact");

            string firstName = membership.Member.FirstName;
            string lastName = membership.Member.LastName;
            string homePhone = membership.Member.HomePhone;
            string mobilePhone = membership.Member.MobilePhone;
            string emailAddress = membership.Member.EmailAddress;

            this._tracer.Trace("firstName={0}, lastName={1}, homePhone={2}, mobilePhone={3}, emailAddress={4}",
                firstName, lastName, homePhone, mobilePhone, emailAddress);

            List<Contact> contacts = base.GetContactsByName(firstName, lastName);
            Contact match = mc.MatchToExistingContact(contacts, mobilePhone, homePhone, emailAddress);

            membership.Member.ContactId = match.ContactId; // need this, otherwise the update fails!
            membership.Member.MatchCode = match.MatchCode;  // set the match code

            this._tracer.Trace("MatchCode={0}", membership.Member.MatchCode);

            // create or update contact
            membership.Member.ContactId = base.CreateOrUpdateContact(membership.Member, false);
            return membership.Member;
        }
        #endregion

        #region crm retrieve
        /// <summary>
        /// Retrieves a single Membership from CRM based on the Membership ID for renewal
        /// We need to check for a Membership that is Pending Renewal, has no Payment Received Date, and has no Paid Invoice
        /// </summary>
        public Membership GetMembership(Guid membershipId)
        {
            QueryExpression query = new QueryExpression { EntityName = "mag_membership", ColumnSet = new ColumnSet("mag_idnumber") };
            query.Criteria.AddCondition("mag_membershipid", ConditionOperator.Equal, membershipId);
            query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, (int)MembershipStatusCode.PendingRenewal);
            query.Criteria.AddCondition("mag_paymentreceived", ConditionOperator.Null);


            // get the latest membership
            query.AddOrder("createdon", OrderType.Descending);

            // join on the related contact
            LinkEntity contact = new LinkEntity
            {
                LinkFromEntityName = "mag_membership",
                LinkToEntityName = "contact",
                LinkFromAttributeName = "mag_contactid",
                LinkToAttributeName = "contactid",
                EntityAlias = "contact",
                Columns = new ColumnSet("contactid", "mag_salutationcode", "gendercode", "firstname", "lastname", "telephone2",
                                        "telephone3", "mobilephone", "emailaddress1", "address1_line1", "address1_line2",
                                        "address1_line3", "address1_city", "address1_postalcode", "birthdate", "mag_yearofbirth")
            };

            query.LinkEntities.Add(contact);

            // join to related product (membership type)
            LinkEntity product = new LinkEntity
            {
                LinkFromEntityName = "mag_membership",
                LinkToEntityName = "product",
                LinkFromAttributeName = "mag_membershiptypeid",
                LinkToAttributeName = "productid",
                EntityAlias = "product",
                Columns = new ColumnSet("productid", "name", "price")
            };

            query.LinkEntities.Add(product);

            // join to an invoice and check 
            LinkEntity invoice = new LinkEntity
            {
                LinkFromEntityName = "mag_membership",
                LinkToEntityName = "invoice",
                LinkFromAttributeName = "mag_invoiceid",
                LinkToAttributeName = "invoiceid",
            };

            invoice.LinkCriteria.AddCondition("statecode", ConditionOperator.NotEqual, (int)InvoiceStateCode.Paid);
            query.LinkEntities.Add(invoice);

            EntityCollection results = this._sdk.RetrieveMultiple(query);
            this._tracer.Trace("results.Entities.Count={0}", results.Entities.Count);

            Membership membership = (from e in results.Entities.ToList()
                                     select new Membership
                                     {
                                         MembershipId = e.Id,
                                         MembershipNumber = e.Get<string>("mag_idnumber"),
                                         IsRegularGift = e.Get<bool>("mag_isregulargift"),
                                         Member = new Contact
                                         {
                                             ContactId = e.Get<Guid>("contact.contactid"),
                                             SalutationName = e.Attributes.ContainsKey("contact.mag_salutationcode") ? e.FormattedValues["contact.mag_salutationcode"] : "",
                                             SalutationCode = e.Get<int>("contact.mag_salutationcode"),
                                             Gender = e.Get<int>("contact.gendercode"),
                                             FirstName = e.Get<string>("contact.firstname"),
                                             LastName = e.Get<string>("contact.lastname"),
                                             HomePhone = e.Get<string>("contact.telephone2"),
                                             WorkPhone = e.Get<string>("contact.telephone3"),
                                             MobilePhone = e.Get<string>("contact.mobilephone"),
                                             EmailAddress = e.Get<string>("contact.emailaddress1"),
                                             Street = e.Get<string>("contact.address1_line1"),
                                             Street2 = e.Get<string>("contact.address1_line2"),
                                             Suburb = e.Get<string>("contact.address1_line3"),
                                             City = e.Get<string>("contact.address1_city"),
                                             PostalCode = e.Get<string>("contact.address1_postalcode"),
                                             DateOfBirth = ConvertToLocalTime(e.GetNullable<DateTime>("contact.birthdate")),
                                             YearOfBirth = e.Get<int>("contact.mag_yearofbirth")
                                         },
                                         MembershipType = new MembershipType
                                         {
                                             MembershipTypeId = e.Get<Guid>("product.productid"),
                                             Name = e.Get<string>("product.name"),
                                             Price = e.Get<decimal>("product.price"),
                                         }
                                     }).Take(1).SingleOrDefault();

            return membership;
        }

        /// <summary>
        /// Converts a CRM UTC DateTime? into local time
        /// </summary>
        private DateTime? ConvertToLocalTime(DateTime? date)
        {
            if (date != null && date.Value != default(DateTime))
            {
                return date.Value.ToLocalTime();
            }

            return null;
        }

        /// <summary>
        /// Gets the latest active membership of a member.
        /// </summary>
        public Guid GetLatestActiveMembershipId(string membershipNumber, bool ignoreLifeTimeMemberships = false)
        {
            QueryExpression query = new QueryExpression { EntityName = "mag_membership" };
            query.Criteria.AddCondition("mag_idnumber", ConditionOperator.Equal, membershipNumber);
            //query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, 809730001); // active
            query.Criteria.AddCondition("statuscode", ConditionOperator.In, new object[] { 809730001, 809730005 }); // active OR service end - PN - 27/11/13

            query.AddOrder("mag_startdate", OrderType.Descending);

            // this is used by the renewal process, lifetime memberships dont need to be renewd
            if (ignoreLifeTimeMemberships)
            {
                LinkEntity le = query.AddLink("product", "mag_membershiptypeid", "productid", JoinOperator.Inner);
                //le.LinkCriteria.AddCondition("name", ConditionOperator.NotEqual, "Lifetime Membership");
                LinkEntity unit = le.AddLink("uom", "defaultuomid", "uomid", JoinOperator.Inner); // Needs to look at the Unit name, as many products can be lifetime memberships - PN - 27/11/13
                unit.LinkCriteria.AddCondition("name", ConditionOperator.NotEqual, "Lifetime");
            }

            EntityCollection results = this._sdk.RetrieveMultiple(query);

            if (results != null && results.Entities != null && results.Entities.Count > 0)
            {
                return results[0].Get<Guid>("mag_membershipid");
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Get a list of Memberships by Contact, Membership Number, and assocaited "Unpaid" Invoice
        /// </summary>
        public List<Membership> GetMemberships(Guid contactId, string membershipNumber)
        {
            QueryExpression query = new QueryExpression { EntityName = "mag_membership" };
            query.Criteria.AddCondition("mag_contactid", ConditionOperator.Equal, contactId);
            query.Criteria.AddCondition("mag_idnumber", ConditionOperator.Equal, membershipNumber);

            // get the latest membership
            query.AddOrder("createdon", OrderType.Descending);

            // link to associated invoice
            LinkEntity invoice = new LinkEntity
            {
                LinkFromEntityName = "mag_membership",
                LinkToEntityName = "invoice",
                LinkFromAttributeName = "mag_invoiceid",
                LinkToAttributeName = "invoiceid",
                EntityAlias = "invoice",
            };

            query.LinkEntities.Add(invoice);

            EntityCollection results = this._sdk.RetrieveMultiple(query);
            List<Membership> memberships = (from e in results.Entities.ToList()
                                            select new Membership
                                            {
                                                MembershipId = e.Id,
                                            }).ToList();

            return memberships;
        }

        /// <summary>
        /// Gets a list of Membership Types (Products) from CRM where "Member Type" = Individual/Family
        /// </summary>
        public List<MembershipType> GetMembershipTypes()
        {
            QueryExpression query = new QueryExpression { EntityName = "product", ColumnSet = new ColumnSet("name", "mag_availableforcode", "price", "description") };
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);   // active only
            query.Criteria.AddCondition("producttypecode", ConditionOperator.Equal, (int)ProductTypeCode.Membership);
            query.Criteria.AddCondition("mag_membertypecode", ConditionOperator.Equal, (int)MemberTypeCode.IndividualFamily);
            query.Criteria.AddCondition("mag_publishtoweb", ConditionOperator.Equal, true);

            EntityCollection results = this._sdk.RetrieveMultiple(query);
            List<MembershipType> types = (from e in results.Entities.ToList()
                                          select new MembershipType
                                          {
                                              MembershipTypeId = e.Id,
                                              Name = e.Get<string>("name"),
                                              AvailableFor = e.FormattedValues["mag_availableforcode"] ?? "",
                                              Price = e.Get<decimal>("price"),
                                              Description = e.Get<string>("description")
                                          }).ToList();

            return types.OrderBy(t => t.Name).ToList();
        }

        /// <summary>
        /// Gets a list of Coastguard Codes from CRM where "Applies To" = Coastguard Region OR Coastguard Unit
        /// </summary>
        public List<Unit> GetUnits()
        {
            object[] codes = new object[] { (int)TagAppliesToCode.CoastguardRegion, (int)TagAppliesToCode.CoastguardUnit };

            QueryExpression query = new QueryExpression { EntityName = "mag_tag" };
            query.Criteria.AddCondition("mag_appliestocode", ConditionOperator.In, codes);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);   // active only

            var link1 = query.AddLink("businessunit", "mag_coastguardregionid", "businessunitid", JoinOperator.LeftOuter);
            link1.EntityAlias = "region";
            link1.Columns = new ColumnSet("name");

            var link2 = query.AddLink("mag_coastguardunit", "mag_coastguardunitid", "mag_coastguardunitid", JoinOperator.LeftOuter);
            link2.EntityAlias = "unit";
            link2.Columns = new ColumnSet("mag_coastguardregionid");

            query.AddOrder("mag_name", OrderType.Ascending);

            EntityCollection results = this._sdk.RetrieveMultiple(query);
            List<Unit> units = (from e in results.Entities.ToList()
                                select new Unit
                                {
                                    UnitCodeId = e.Id,
                                    Name = e.Get<string>("mag_name"),
                                    Region = e.Contains("region.name") ? e.Get<string>("region.name")
                                    : e.Contains("unit.mag_coastguardregionid") ? ((EntityReference)e.GetAttributeValue<AliasedValue>("unit.mag_coastguardregionid").Value).Name : ""
                                }).ToList();

            return units.OrderBy(u => u.Name).ToList();
        }
        #endregion
    }
}