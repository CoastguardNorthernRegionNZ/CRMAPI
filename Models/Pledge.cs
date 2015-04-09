using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Coastguard.Web.API.Models
{
    public class Pledge
    {
        [DataMember(IsRequired = true)]
        public int PaymentFrequencyCode { get; set; }

        [DataMember(IsRequired = true)]
        public DateTime? StartDate { get; set; }

        public Guid PledgeId { get; set; }

        public string DpsBillingId { get; set; }

        public DateTime? EndDate { get; set; }

        public PledgeMatchCode MatchCode { get; set; }
    }
}