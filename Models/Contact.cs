using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Coastguard.Web.API.Models
{
    public class Contact
    {
        [DataMember(IsRequired = true)]
        public int SalutationCode { get; set; }

        public string SalutationName { get; set; }

        [DataMember(IsRequired = true)]
        public string FirstName { get; set; }

        [DataMember(IsRequired = true)]
        public string LastName { get; set; }

        [DataMember(IsRequired = true)]
        public string EmailAddress { get; set; }

        public Guid ContactId { get; set; }

        public ContactMatchCode MatchCode { get; set; }

        public string NameOnTaxReceipt { get; set; }

        public int Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public int YearOfBirth { get; set; }

        public string CompanyName { get; set; }

        public string HomePhone { get; set; }

        public string MobilePhone { get; set; }

        public string WorkPhone { get; set; }

        public string Street { get; set; }

        public string Street2 { get; set; }

        public string Suburb { get; set; }

        public string City { get; set; }

        //[DataMember(Name = "region")]
        //public string Region { get; set; }

        public string PostalCode { get; set; }

        //[DataMember(Name = "source_family_friend")]
        //public bool SourcedByFamilyOrFriend { get; set; }

        //[DataMember(Name = "source_website")]
        //public bool SourcedByWebsite { get; set; }

        //[DataMember(Name = "source_is_member")]
        //public bool IsMember { get; set; }

        //[DataMember(Name = "source_text")]
        //public string SourceText { get; set; }

        public Guid OwnerId { get; set; }

        public string OwnerLogicalName { get; set; }    // systemuser or team

        public string FullName
        {
            get
            {
                return string.Join(" ", this.FirstName, this.LastName);
            }
            set { }
        }

        public bool IsDonor { get; set; }
    }
}