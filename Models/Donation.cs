using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Coastguard.Web.API.Models
{
    public class Donation
    {
        [DataMember(IsRequired = true), ]
        public decimal Amount { get; set; }

        [DataMember(IsRequired = true)]
        public DateTime Date { get; set; }

        [DataMember(IsRequired = true)]
        public bool IsRegularGift { get; set; }

        [DataMember(IsRequired = true)]
        public string DpsResponseText { get; set; }

        [DataMember(IsRequired = true)]
        public string DpsTransactionReference { get; set; }

        [DataMember(IsRequired = true)]
        public string CCExpiry { get; set; }

        [DataMember(IsRequired = true)]
        public int RetryCount { get; set; }

        [DataMember(IsRequired = true)]
        public Pledge Pledge { get; set; }

        [DataMember(IsRequired = true)]
        public Contact Donor { get; set; }

        public bool DpsPaymentSuccessful { get; set; }

        public string RegionCode { get; set; }

        public Guid CampaignId { get; set; }

        public Guid UnitId { get; set; }

        public string Comments { get; set; }

        public Guid ReasonForHelpingId { get; set; }

        public string Region { get; set; }
    }
}