using Coastguard.Data;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Coastguard.Web.API.Controllers
{
    public static class MappingController
    {
        public static EntityReference MapToEntityReference(string logicalName, Guid id)
        {
            EntityReference reference = null;

            if (!string.IsNullOrEmpty(logicalName) && id != default(Guid))
            {
                reference = new EntityReference(logicalName, id);
            }

            return reference;
        }

        public static OptionSetValue MapToOptionSetValue(int code)
        {
            OptionSetValue osValue = null;

            if (code != default(int))
            {
                osValue = new OptionSetValue(code);
            }

            return osValue;
        }

        public static OptionSetValue MapGenderToOptionSetValue(char gender)
        {
            OptionSetValue value = null;

            switch (gender)
            {
                case 'M':
                    value = new OptionSetValue(1);
                    break;

                case 'F':
                    value = new OptionSetValue(2);
                    break;

                default:
                    value = null;
                    break;
            }

            return value;
        }
    }
}