using Coastguard.Web.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Coastguard.Web.API.Interfaces
{
    [ServiceContract(Namespace = "http://placeholder/v1")]
    public interface IDonationService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "/ping", ResponseFormat = WebMessageFormat.Json, Method = "GET")]
        string Ping();

        [OperationContract]
        [WebInvoke(UriTemplate = "/page/{regionCode}", ResponseFormat = WebMessageFormat.Json, Method = "GET")]
        PageData GetPageData(string regionCode);

        [OperationContract]
        [WebInvoke(UriTemplate = "/create", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST")]
        void PostDonation(Donation d);
    }
}
