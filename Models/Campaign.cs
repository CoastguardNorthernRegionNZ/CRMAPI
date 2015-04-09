using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Coastguard.Web.API.Models
{
    public class Campaign
    {
        public Guid CampaignId { get; set; }

        public string Name { get; set; }
    }
}