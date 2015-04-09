using Coastguard.Data;
using Coastguard.Web.API.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Coastguard.Web.API.Controllers
{
    public class PageController
    {
        #region private variables
        private IOrganizationService _sdk;
        private Tracer _tracer = null;
        #endregion

        #region constructors
        public PageController(IOrganizationService sdk, Tracer tracer)
        {
            this._sdk = sdk;
            this._tracer = tracer;
        }
        #endregion

        #region methods
        public PageData GetDonationPageData(string regionCode)
        {
            this._tracer.Trace("Method: PageController.GetPageData");
            this._tracer.Trace("Parameters: regionCode={0}", regionCode);

            DonationController dc = new DonationController(this._sdk, this._tracer);

            PageData data = new PageData
            {
                Campaigns = dc.GetCampaigns(regionCode),
                Regions = dc.GetRegions(),
                ReasonsForHelping = dc.GetReasonsForHelping(),
                PaymentFrequencies = dc.GetOptionSet("mag_pledge", "mag_paymentfrequencycode"),
                Salutations = dc.GetOptionSet("contact", "mag_salutationcode")
            };

            return data;
        }

        public PageData GetMembershipPageData(string regionCode)
        {
            this._tracer.Trace("Method: PageController.GetMembershipPageData");
            this._tracer.Trace("Parameters: regionCode={0}", regionCode);

            MembershipController mc = new MembershipController(this._sdk, this._tracer);

            PageData data = new PageData
            {
                MembershipTypes = mc.GetMembershipTypes(),
                Salutations = mc.GetOptionSet("contact", "mag_salutationcode"),
                Genders = mc.GetOptionSet("contact", "gendercode"),
                Units = mc.GetUnits()
            };

            return data;
        }
        #endregion
    }
}