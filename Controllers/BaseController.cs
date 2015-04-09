using Coastguard.Data;
using Coastguard.Web.API.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Coastguard.Web.API.Controllers
{
    public class BaseController
    {
        #region initialize
        private IOrganizationService _sdk = null;
        private Tracer _tracer = null;

        public BaseController() { }

        public BaseController(IOrganizationService sdk, Tracer tracer)
        {
            this._sdk = sdk;
            this._tracer = tracer;
        }
        #endregion

        #region contacts
        public List<Contact> GetContactsByName(string firstName, string lastName)
        {
            this._tracer.Trace("Method: DonationController.GetContactsByName");
            this._tracer.Trace("Parameters: firstName={0}, lastName={1}", firstName, lastName);

            QueryExpression query = new QueryExpression { EntityName = "contact", ColumnSet = new ColumnSet("firstname", "lastname", "emailaddress1", "telephone2", "mobilephone", "ownerid") };
            query.Criteria.AddCondition("firstname", ConditionOperator.Equal, firstName);
            query.Criteria.AddCondition("lastname", ConditionOperator.Equal, lastName);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);   // active

            query.AddOrder("createdon", OrderType.Ascending);   // order by created on date

            this._tracer.Trace("Retrieving Contacts");
            EntityCollection results = this._sdk.RetrieveMultiple(query);
            this._tracer.Trace("Number of Contacts found={0}", results.Entities.Count);

            List<Contact> contacts = (from e in results.Entities.ToList()
                                      select new Contact
                                      {
                                          ContactId = e.Id,
                                          FirstName = e.Get<string>("firstname"),
                                          LastName = e.Get<string>("lastname"),
                                          EmailAddress = e.Get<string>("emailaddress1"),
                                          HomePhone = e.Get<string>("telephone2"),
                                          MobilePhone = e.Get<string>("mobilephone"),
                                          OwnerId = e.Get<EntityReference>("ownerid").Id
                                      }).ToList();

            return contacts;
        }

        public Guid CreateOrUpdateContact(Contact contact, bool isDonation) // This is shit but we need a way of knowing whether to set membership specific fields
        {
            this._tracer.Trace("Method: BaseController.CreateOrUpdateContact");

            Guid contactId = default(Guid);
            ContactMatchCode code = contact.MatchCode;

            if (code == ContactMatchCode.None)
            {
                this._tracer.Trace("MatchCode=None. Creating a new Contact");

                // create a new contact
                contactId = this.CreateContact(contact, isDonation);
                this._tracer.Trace("Contact {0} created", contact.FullName);
            }
            else
            {
                this._tracer.Trace("MatchCode={0}. Updating existing Contact", code.ToString());

                // update existing contact
                contactId = contact.ContactId;
                this.UpdateContact(contact, isDonation);
            }

            return contactId;
        }

        /// <summary>
        /// Update the Contact details for a Membership
        /// </summary>
        private void UpdateContact(Contact c, bool isDonation)
        {
            this._tracer.Trace("Method: DonationController.UpdateContact");

            Entity contact = new Entity("contact");
            contact["contactid"] = c.ContactId;
            contact["mag_salutationcode"] = MappingController.MapToOptionSetValue(c.SalutationCode);
            if (isDonation) { contact["mag_donor"] = c.IsDonor; } // THis is shit but we dont want to set IsDonor to no just cause its a membership

            // memberships
            contact["firstname"] = c.FirstName;
            contact["lastname"] = c.LastName;
            contact["gendercode"] = MappingController.MapToOptionSetValue(c.Gender);
            if (c.YearOfBirth > 1900) { contact["mag_yearofbirth"] = c.YearOfBirth; }
            contact["emailaddress1"] = c.EmailAddress;
            contact["address1_line1"] = c.Street;
            contact["address1_line2"] = c.Street2;
            contact["address1_line3"] = c.Suburb;
            contact["address1_city"] = c.City;
            contact["address1_postalcode"] = c.PostalCode;
            contact["telephone3"] = c.WorkPhone;
            contact["telephone2"] = c.HomePhone;
            contact["mobilephone"] = c.MobilePhone;

            // only set the date of birth if a value has been passed through
            if (c.DateOfBirth.HasValue && c.DateOfBirth.Value != default(DateTime))
            {
                contact["birthdate"] = c.DateOfBirth;
            }

            this._sdk.Update(contact);
        }

        public Guid CreateContact(Contact c, bool isDonation)
        {
            this._tracer.Trace("Method: DonationController.CreateContact");

            Entity contact = new Entity("contact");
            contact["mag_salutationcode"] = MappingController.MapToOptionSetValue(c.SalutationCode);
            contact["firstname"] = c.FirstName;
            contact["lastname"] = c.LastName;
            contact["mag_organisationid"] = MappingController.MapToEntityReference("account", this.GetAccountId(c.CompanyName));

            if (isDonation) { contact["mag_donor"] = c.IsDonor; }
            else { contact["donotbulkemail"] = false; } // CNR allow bulk email by default

            contact["gendercode"] = MappingController.MapToOptionSetValue(c.Gender);
            contact["birthdate"] = c.DateOfBirth;
            if (c.DateOfBirth.HasValue) { contact["mag_yearofbirth"] = c.DateOfBirth.Value.Year; }

            contact["emailaddress1"] = c.EmailAddress;
            contact["telephone3"] = c.WorkPhone;
            contact["telephone2"] = c.HomePhone;
            contact["mobilephone"] = c.MobilePhone;

            contact["address1_line1"] = c.Street;
            contact["address1_line2"] = c.Street2;
            contact["address1_line3"] = c.Suburb;
            contact["address1_city"] = c.City;
            contact["address1_postalcode"] = c.PostalCode;

            //contact["mag_importsource"] = "Online Donation";  // todo: uncomment once the field has been created in crm
            //contact["mag_matchcode"] = c.MatchCode.ToString(); // todo: uncomment once the field has been created in crm

            // note: design doc says to pass in job title but api data dictionary doesn't include this field

            /* fields don't exist in crm yet
            contact["mag_sourcefamilyfriend"] = d.SourcedByFamilyOrFriend;
            contact["mag_sourceismember"] = d.IsMember;
            contact["mag_sourcewebsite"] = d.SourcedByWebsite;
            contact["mag_sourcetext"] = d.SourceText;
             */

            return this._sdk.Create(contact);
        }

        #endregion

        #region accounts
        private Guid GetAccountId(string name)
        {
            Guid accountId = default(Guid);

            if (!string.IsNullOrEmpty(name))
            {

                QueryByAttribute query = new QueryByAttribute { EntityName = "account" };   // no columnset, we just want the id
                query.AddAttributeValue("name", name);

                EntityCollection results = this._sdk.RetrieveMultiple(query);
                if (results.Entities.Count > 0)
                {
                    accountId = results[0].Id;
                }
            }

            return accountId;
        }
        #endregion

        #region metadata
        public Dictionary<int, string> GetOptionSet(string entityName, string fieldName)
        {
            this._tracer.Trace("Method: GetOptionSet");
            this._tracer.Trace("Parameters: entityName={0}, fieldName={1}", entityName, fieldName);

            Dictionary<int, string> options = new Dictionary<int, string>();

            try
            {
                RetrieveAttributeRequest request = new RetrieveAttributeRequest
                {
                    EntityLogicalName = entityName,
                    LogicalName = fieldName,
                    RetrieveAsIfPublished = true
                };

                this._tracer.Trace("Getting OptionSet");
                RetrieveAttributeResponse response = (RetrieveAttributeResponse)this._sdk.Execute(request);
                this._tracer.Trace("OptionSet retrieved");

                if (response != null && response.Results != null)
                {
                    PicklistAttributeMetadata pam = (PicklistAttributeMetadata)response.AttributeMetadata;
                    OptionMetadata[] omd = pam.OptionSet.Options.ToArray();

                    omd.ToList()
                        .ForEach(o =>
                        {
                            options.Add(o.Value.Value, o.Label.UserLocalizedLabel.Label);
                        });
                }

                this._tracer.Trace("Number of options found: {0}", options.Count);
            }
            catch (Exception ex)
            {
                this._tracer.Trace("Unable to retrieve OptionSet from CRM");
                this._tracer.Trace(ex.ToString());
            }

            return options;
        }
        #endregion

        #region helpers
        public DateTime? GetCCExpiryDate(string expiry)
        {
            this._tracer.Trace("Method: BaseController.GetCCExpiryDate");
            this._tracer.Trace("Parameters: expiry={0}", expiry);

            // example: CC Expiry Date from DPS is 0315 (march 2015)

            DateTime? expiryDate = null;

            if (!string.IsNullOrEmpty(expiry) && expiry.Length == 4)
            {
                int month = int.Parse(expiry.Substring(0, 2));
                int year = int.Parse(expiry.Substring(2, 2));

                int currentYear = DateTime.Now.Year;

                // figure out the actual year that the card should expiry. It will be in the future since it's been processed by DPS
                int fullYear = int.Parse(string.Concat(currentYear.ToString().Substring(0, 2), year));

                // might need to make adjustments. E.g. if current year is 2998 and the CCExpiryDate is 0302, the date should be March 3002
                if (fullYear < currentYear)
                {
                    fullYear += 100;
                }

                expiryDate = new DateTime(fullYear, month, DateTime.DaysInMonth(fullYear, month));
            }

            this._tracer.Trace("expiryDate={0}", expiryDate);

            return expiryDate;
        }
        #endregion
    }
}