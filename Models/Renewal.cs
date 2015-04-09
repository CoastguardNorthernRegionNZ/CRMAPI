using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Coastguard.Web.API.Models
{
    /// <summary>
    /// Replaced by the 'MembershipRenewal.cs' model, to include full membership type instead of just the amount
    /// </summary>
    public class Renewal
    {
        public string MembershipNumber { get; set; }
        public decimal Amount { get; set; }
    }
}