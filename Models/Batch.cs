using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Coastguard.Web.API.Models
{
    public class Batch
    {
        public Guid BatchId { get; set; }
        public BatchMatchCode MatchCode { get; set; }
    }
}
