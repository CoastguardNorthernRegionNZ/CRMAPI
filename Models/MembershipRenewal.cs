using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Coastguard.Web.API.Models
{
    /// <summary>
    /// Replaces the 'Renewal.cs' model, to include full membership type instead of just the amount
    /// </summary>
    public class MembershipRenewal
    {
        public string MembershipNumber { get; set; }
        public MembershipType MembershipType { get; set; }
    }
}