using Coastguard.Data;
using Coastguard.Web.API.Controllers;
using Coastguard.Web.API.Interfaces;
using Coastguard.Web.API.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web.Script.Serialization;

namespace Coastguard.Web.API.Services
{
    public class DonationService : IDonationService
    {
        #region private variables
        private Tracer _tracer = null;
        private string _logPath = null;
        #endregion

        #region constructors
        public DonationService()
        {
            this._tracer = new Tracer(null);
            this._logPath = ConfigurationManager.AppSettings["log.path"];
        }
        #endregion

        #region GET
        public string Ping()
        {
            return "Hello from the Coastguard Donations Service";
        }

        public PageData GetPageData(string regionCode)
        {
            this._tracer.Trace("Method: Service.GetPageData");
            this._tracer.Trace("Parameters: regionCode={0}", regionCode);
            PageData data = null;

            try
            {
                var sdk = ConnectionController.ConnectToCrm(this._tracer);
                if (sdk != null)
                {
                    PageController page = new PageController(sdk, this._tracer);
                    data = page.GetDonationPageData(regionCode);
                }
                else
                {
                    string message = "Unable to connect to CRM. Check web.config";
                    this._tracer.Trace(message);
                    throw new Exception(message);
                }
            }
            catch (FaultException<OrganizationServiceFault> fe)
            {
                if (fe.Detail != null)
                {
                    this._tracer.Trace(fe.Detail.ToString());
                }

                this._tracer.Trace(fe.ToString());
                throw new WebFaultException<Error>(ConvertToError(fe, DateTime.Now.Ticks.ToString()), HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                this._tracer.Trace(ex.ToString());
                throw new WebFaultException<Error>(ConvertToError(ex, DateTime.Now.Ticks.ToString()), HttpStatusCode.InternalServerError);
            }
            finally
            {
                // write to the log file
                this._tracer.WriteToLog(this._logPath);
            }

            return data;
        }
        #endregion

        #region POST
        public void PostDonation(Donation donation)
        {
            this._tracer.Trace("Method: Service.PostDonation");

            try
            {
                this.LogDonationInfo(donation);

                if (this.IsValidInput(donation))
                {
                    var sdk = ConnectionController.ConnectToCrm(this._tracer);
                    if (sdk != null)
                    {
                        DonationController dc = new DonationController(sdk, this._tracer);
                        dc.ProcessDonation(donation);
                        this._tracer.Trace("Donation processed");
                    }
                    else
                    {
                        string message = "Unable to connect to CRM. Check web.config";
                        this._tracer.Trace(message);
                        throw new Exception(message);
                    }
                }
                else
                {
                    throw new Exception("Invalid input. The Donor and Pledge cannot be null for a Donation");
                }
            }
            catch (FaultException<OrganizationServiceFault> fe)
            {
                if (fe.Detail != null)
                {
                    this._tracer.Trace(fe.Detail.ToString());
                }

                this._tracer.Trace(fe.ToString());

                string reference = donation != null ? donation.DpsTransactionReference : "Donation is Null";
                throw new WebFaultException<Error>(ConvertToError(fe, reference), HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                this._tracer.Trace(ex.ToString());

                string reference = donation != null ? donation.DpsTransactionReference : "Donation is Null";
                throw new WebFaultException<Error>(ConvertToError(ex, reference), HttpStatusCode.InternalServerError);
            }
            finally
            {
                // write to the log file
                this._tracer.WriteToLog(this._logPath);
            }
        }
        #endregion

        #region validation
        private bool IsValidInput(Donation donation)
        {
            // every donation requires a "Donor" and a "Pledge"
            bool isValid = false;

            if (donation != null)
            {
                if (donation.Donor != null && donation.Pledge != null)
                {
                    isValid = true;
                }
            }

            return isValid;
        }
        #endregion

        #region logging and error handling
        private Error ConvertToError(Exception ex, string id)
        {
            return new Error
            {
                Id = id,
                Description = ex.Message,
                StackTrace = string.Format("{0}\r\n\r\n{1}", ex.ToString(), ex.InnerException != null ? ex.InnerException.ToString() : "")
            };
        }

        private void LogDonationInfo(Donation donation)
        {
            JavaScriptSerializer js = new JavaScriptSerializer();
            string rawData = js.Serialize(donation);
            _tracer.Trace("{0}", rawData);
        }
        #endregion
    }
}
