using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Coastguard.Web.API.Models
{
    public class MembershipType
    {
        public Guid MembershipTypeId { get; set; }
        public string Name { get; set; }
        public string AvailableFor { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
    }
}
