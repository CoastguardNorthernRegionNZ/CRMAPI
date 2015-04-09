using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Coastguard.Web.API.Models
{
    public class ReasonForHelping
    {
        public Guid ReasonForHelpingId { get; set; }

        public string Name { get; set; }
    }
}
