using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Coastguard.Web.API.Models
{
    public class Unit
    {
        public Guid UnitCodeId { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
    }
}
