using Coastguard.Web.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Coastguard.Web.API.Interfaces
{
    [ServiceContract(Namespace = "http://placeholder/v1")]
    public interface IMembershipService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "/ping", ResponseFormat = WebMessageFormat.Json, Method = "GET")]
        string Ping();

        [OperationContract]
        [WebInvoke(UriTemplate = "/page/{regionCode}", ResponseFormat = WebMessageFormat.Json, Method = "GET")]
        PageData GetPageData(string regionCode);

        [OperationContract]
        [WebInvoke(UriTemplate = "/{membershipId}", ResponseFormat = WebMessageFormat.Json, Method = "GET")]
        Membership Get(string membershipId);

        [OperationContract]
        [WebInvoke(UriTemplate = "/create", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST")]
        void Create(Membership m);

        // This endpoint has been replaced by /renewmembership, to handle the entire membership type, instead of just the amount
        [OperationContract]
        [WebInvoke(UriTemplate = "/renew", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST")]
        void Renew(Renewal renewal);

        [OperationContract]
        [WebInvoke(UriTemplate = "/renewmembership", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST")]
        void RenewMembership(MembershipRenewal renewal);

        [OperationContract]
        [WebInvoke(UriTemplate = "/update", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, Method = "POST")]
        void Update(Membership m);
    }
}
