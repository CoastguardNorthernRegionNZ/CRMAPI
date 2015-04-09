using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Coastguard.Web.API.Models
{
    public class Error
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string StackTrace { get; set; }
    }
}