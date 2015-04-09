using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Coastguard.Web.API.Models
{
    public class Membership
    {
        // internal CRM GUID
        public Guid MembershipId { get; set; }

        [DataMember(IsRequired = true)]
        public string MembershipNumber { get; set; }

        [DataMember(IsRequired = true)]
        public MembershipType MembershipType { get; set; }

        [DataMember(IsRequired = true)]
        public Contact Member { get; set; }

        public Guid RegionCodeId { get; set; }
        public Guid UnitCodeId { get; set; }
        public MembershipMatchCode MatchCode { get; set; }

        // dps
        [DataMember(IsRequired = true)]
        public bool IsRegularGift { get; set; }

        [DataMember(IsRequired = true)]
        public string DpsTransactionReference { get; set; }

        [DataMember(IsRequired = true)]
        public string CCExpiry { get; set; }

        // only required for regular gifts
        public string DpsBillingId { get; set; }
    }
}
