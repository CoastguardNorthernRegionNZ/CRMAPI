using Coastguard.Data;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Coastguard.Web.API.Controllers
{
    public static class ConnectionController
    {
        #region initialize
        private static IOrganizationService _sdk = null;
        private static DateTime _lastConnectedOn;

        public static bool IsConnected
        {
            get
            {
                return _sdk != null;
            }
        }

        public static DateTime LastConnectedOn
        {
            get
            {
                return _lastConnectedOn;
            }
        }
        #endregion

        public static IOrganizationService ConnectToCrm(Tracer tracer)
        {
            tracer.Trace("Connecting to CRM");
            _lastConnectedOn = DateTime.Now;

            try
            {
                _sdk = CrmConnection.Connect(ConfigurationManager.AppSettings["crm.sdkurl"],
                        ConfigurationManager.AppSettings["crm.domain"],
                        ConfigurationManager.AppSettings["crm.username"],
                        ConfigurationManager.AppSettings["crm.password"],
                        ConfigurationManager.AppSettings["crm.org"]);

                tracer.Trace("Connected successfully");
            }
            catch (Exception ex)
            {
                tracer.Trace("Unable to connect to CRM");
                tracer.Trace(ex.ToString());
            }

            return _sdk;
        }
    }
}