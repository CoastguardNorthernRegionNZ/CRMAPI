using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Coastguard.Web.API.Models
{
    public class PageData
    {
        // donations
        public List<Campaign> Campaigns { get; set; }
        public List<Region> Regions { get; set; }
        public List<ReasonForHelping> ReasonsForHelping { get; set; }
        public Dictionary<int, string> PaymentFrequencies { get; set; }

        // memberships
        public List<MembershipType> MembershipTypes { get; set; }
        public Dictionary<int, string> Genders { get; set; }
        public List<Unit> Units { get; set; }

        // generic
        public Dictionary<int, string> Salutations { get; set; }
    }
}