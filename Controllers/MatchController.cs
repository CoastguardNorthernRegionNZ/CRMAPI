using Coastguard.Data;
using Coastguard.Web.API.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Coastguard.Web.API.Controllers
{
    /// <summary>
    /// Finds an existing Contact in CRM based on first name, last name, mobile phone, home phone, email address
    /// Finds an existing Pledge based on it's primary key
    /// Finds an existing Batch
    /// </summary>

    public class MatchController
    {
        #region initialize
        private Tracer _tracer = null;

        public MatchController(Tracer tracer)
        {
            this._tracer = tracer;
        }
        #endregion

        #region methods
        /// <summary>
        /// Looks for an existing contact based on their first name, last name, mobile/home phone and email address
        /// </summary>
        public Contact MatchToExistingContact(List<Contact> contacts, string mobilePhone, string homePhone, string emailAddress)
        {
            this._tracer.Trace("Method: Matcher.MatchToExistingContact");
            this._tracer.Trace("Parameters: contacts, mobilePhone={0}, homePhone={1}, emailAddress={2}", mobilePhone, homePhone, emailAddress);

            Contact contact = new Contact();
            ContactMatchCode code = ContactMatchCode.None;

            mobilePhone = RemoveNonAlphaNumericChars(mobilePhone);
            homePhone = RemoveNonAlphaNumericChars(homePhone);

            // get all active contacts by first name and last name. include emailaddress1, telephone2, mobilephone then filter locally.
            List<Contact> contactsByName = (from c in contacts
                                            select new Contact
                                            {
                                                ContactId = c.ContactId,
                                                FirstName = c.FirstName,
                                                LastName = c.LastName,
                                                EmailAddress = c.EmailAddress,
                                                MobilePhone = RemoveNonAlphaNumericChars(c.MobilePhone),
                                                HomePhone = RemoveNonAlphaNumericChars(c.HomePhone),
                                                OwnerId = c.OwnerId,
                                                OwnerLogicalName = c.OwnerLogicalName
                                            }).ToList();

            if (contactsByName.Count > 0)
            {
                List<Contact> matches = new List<Contact>();

                if (!string.IsNullOrEmpty(emailAddress))
                {
                    // step 1: filter by email address
                    matches = contactsByName.Where(c => !string.IsNullOrEmpty(c.EmailAddress) && c.EmailAddress.Equals(emailAddress, StringComparison.InvariantCultureIgnoreCase)).ToList();
                    if (matches.Count > 0) { code = ContactMatchCode.FirstName | ContactMatchCode.LastName | ContactMatchCode.Email; }
                }

                if (matches.Count == 0)
                {
                    if (!string.IsNullOrEmpty(mobilePhone))
                    {
                        // step 2: match by mobile phone number
                        matches = contactsByName.Where(c => !string.IsNullOrEmpty(c.MobilePhone) && c.MobilePhone.Equals(mobilePhone, StringComparison.InvariantCultureIgnoreCase)).ToList();
                        if (matches.Count > 0) { code = ContactMatchCode.FirstName | ContactMatchCode.LastName | ContactMatchCode.MobilePhone; }
                    }

                    if (matches.Count == 0)
                    {
                        if (!string.IsNullOrEmpty(homePhone))
                        {
                            // step 3: match by home phone number
                            matches = contactsByName.Where(c => !string.IsNullOrEmpty(c.HomePhone) && c.HomePhone.Equals(homePhone, StringComparison.InvariantCultureIgnoreCase)).ToList();
                            if (matches.Count > 0) { code = ContactMatchCode.FirstName | ContactMatchCode.LastName | ContactMatchCode.HomePhone; }
                        }
                    }
                }


                if (matches.Count > 0)
                {
                    contact = matches[0];
                    contact.MatchCode = code;
                }
            }

            this._tracer.Trace("contact.MatchCode={0}", contact.MatchCode.ToString());

            return contact;
        }

        /// <summary>
        /// Looks for an existing Pledge based on the given DPS Billing ID
        /// </summary>
        public Pledge MatchToExistingPledge(List<Pledge> pledges, Guid dpsBillingId)
        {
            this._tracer.Trace("Method: MatchController.MatchToExistingPledge");
            this._tracer.Trace("Parameters: pledges, dpsBillingId={0}", dpsBillingId);

            Pledge pledge = new Pledge { MatchCode = PledgeMatchCode.None };

            if (pledges.Count > 0)
            {
                pledge.PledgeId = pledges[0].PledgeId;
                pledge.MatchCode = PledgeMatchCode.DpsBillingId;
            }

            this._tracer.Trace("pledge.MatchCode={0}", pledge.MatchCode.ToString());

            return pledge;
        }

        /// <summary>
        /// Looks for an existing Pledge based on the ContactID and donation amount
        /// </summary>
        public Pledge MatchToExistingPledge(List<Pledge> pledges, Guid contactId, decimal amount)
        {
            this._tracer.Trace("Method: MatchController.MatchToExistingPledge");
            this._tracer.Trace("Parameters: pledges, contactId={0}, amount={1}", contactId, amount);

            Pledge pledge = new Pledge { MatchCode = PledgeMatchCode.None };

            if (pledges.Count > 0)
            {
                pledge.PledgeId = pledges[0].PledgeId;
                pledge.MatchCode = PledgeMatchCode.Contact | PledgeMatchCode.Amount;
            }

            this._tracer.Trace("pledge.MatchCode={0}", pledge.MatchCode.ToString());

            return pledge;
        }

        /// <summary>
        /// Looks for an existing payment batch in CRM
        /// </summary>
        public Batch MatchToExistingBatch(List<Batch> batches)
        {
            this._tracer.Trace("Method: MatchController.MatchToExistingBatch");

            Batch batch = new Batch { MatchCode = BatchMatchCode.None };

            if (batches.Count > 0)
            {
                batch.BatchId = batches[0].BatchId;
                batch.MatchCode = BatchMatchCode.BatchType | BatchMatchCode.TransactionDate | BatchMatchCode.PaymentFrequency | BatchMatchCode.PaymentMethod;
            }

            this._tracer.Trace("batch.MatchCode={0}", batch.MatchCode.ToString());

            return batch;
        }

        /// <summary>
        /// Looks for an existing membership in CRM
        /// </summary>
        public Membership MatchToExistingMembership(List<Membership> memberships)
        {
            Membership membership = new Membership { MatchCode = MembershipMatchCode.None };

            if (memberships.Count > 0)
            {
                membership.MembershipId = memberships[0].MembershipId;
                membership.MatchCode = MembershipMatchCode.Contact | MembershipMatchCode.MembershipNumber | MembershipMatchCode.UnpaidInvoice;
            }

            return membership;
        }
        #endregion

        #region helpers
        /// <summary>
        /// Removes non-alphanumeric chars from a specific input. E.g. from mobile phone, home phone etc so that we can match values correctly in CRM.
        /// </summary>
        private string RemoveNonAlphaNumericChars(string input)
        {
            this._tracer.Trace("Method: MatchController.RemoveNonAlphaNumericChars");
            this._tracer.Trace("Parameters: input={0}", input);

            string cleaned = string.Empty;

            if (!string.IsNullOrEmpty(input))
            {
                string pattern = @"[^A-Za-z0-9]";
                cleaned = Regex.Replace(input, pattern, string.Empty);
                this._tracer.Trace("cleaned={0}", cleaned);
            }

            return cleaned;
        }
        #endregion
    }
}
