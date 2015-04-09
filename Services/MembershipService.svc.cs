using Coastguard.Data;
using Coastguard.Web.API.Models;
using Coastguard.Web.API.Controllers;
using Coastguard.Web.API.Interfaces;
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
    public class MembershipService : IMembershipService
    {
        #region initialization
        private Tracer _tracer = null;
        private string _logPath = null;

        public MembershipService()
        {
            this._tracer = new Tracer(null);
            this._logPath = ConfigurationManager.AppSettings["log.path"];
        }

        #endregion

        #region GET
        public string Ping()
        {
            return "Hello from the Coastguard Membership Service";
        }

        /// <summary>
        /// Returns Membership Types, Salutations, Genders, and Units for the Coastguard Memberships page
        /// </summary>
        public PageData GetPageData(string regionCode)
        {
            this._tracer.Trace("Method: MembershipService.GetPageData");
            this._tracer.Trace("Parameters: regionCode={0}", regionCode);
            PageData data = null;

            try
            {
                var sdk = ConnectionController.ConnectToCrm(this._tracer);
                if (sdk != null)
                {
                    PageController page = new PageController(sdk, this._tracer);
                    data = page.GetMembershipPageData(regionCode);
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

        /// <summary>
        /// Gets a single Membership based on a Membership ID
        /// </summary>
        public Membership Get(string membershipId)
        {
            Guid id = Guid.Parse(membershipId); // fine if this crashes as the error handling will kick in.

            this._tracer.Trace("Method: MembershipService.Get");
            this._tracer.Trace("Parameters: membershipId={0}", membershipId);
            Membership membership = null;

            try
            {
                if (id != Guid.Empty)
                {
                    var sdk = ConnectionController.ConnectToCrm(this._tracer);
                    if (sdk != null)
                    {
                        MembershipController mc = new MembershipController(sdk, this._tracer);
                        membership = mc.GetMembership(id);

                        this._tracer.Trace("membership is null={0}", membership == null);

                        // if there is no membership found, throw an error back to the caller
                        if (membership == null)
                        {
                            this._tracer.Trace("membership not valid");
                            throw new Exception("Membership is not valid for renewal");
                        }
                    }
                    else
                    {
                        string message = "Unable to connect to CRM. Check web.config";
                        this._tracer.Trace(message);
                        throw new Exception(message);
                    }
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

            return membership;
        }

        #endregion

        #region CREATE
        public void Create(Membership membership)
        {
            this._tracer.Trace("Method: MembershipService.Create");

            try
            {
                this.LogMembershipInfo(membership);

                if (this.IsValidInput(membership))
                {
                    var sdk = ConnectionController.ConnectToCrm(this._tracer);
                    if (sdk != null)
                    {
                        MembershipController mc = new MembershipController(sdk, this._tracer);
                        mc.ProcessCreate(membership);
                        this._tracer.Trace("Membership processed");
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
                    throw new Exception("Invalid input. A Membership must contain a Member and a Membership Type");
                }
            }
            catch (FaultException<OrganizationServiceFault> fe)
            {
                if (fe.Detail != null)
                {
                    this._tracer.Trace(fe.Detail.ToString());
                }

                this._tracer.Trace(fe.ToString());

                string reference = membership != null ? membership.DpsTransactionReference : "Membership is Null";
                throw new WebFaultException<Error>(ConvertToError(fe, reference), HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                this._tracer.Trace(ex.ToString());

                string reference = membership != null ? membership.DpsTransactionReference : "Membership is Null";
                throw new WebFaultException<Error>(ConvertToError(ex, reference), HttpStatusCode.InternalServerError);
            }
            finally
            {
                // write to the log file
                this._tracer.WriteToLog(this._logPath);
            }
        }
        #endregion

        #region RENEW
        public void RenewMembership(MembershipRenewal renewal)
        {
            this._tracer.Trace("Method: MembershipService.RenewMembership: MembershipNumber={0}", renewal.MembershipNumber);

            try
            {
                if (!string.IsNullOrEmpty(renewal.MembershipNumber))
                {
                    var sdk = ConnectionController.ConnectToCrm(this._tracer);
                    if (sdk != null)
                    {
                        _tracer.Trace("membershipTypeId={0}, amount={1}", renewal.MembershipType.MembershipTypeId, renewal.MembershipType.Price);

                        MembershipController mc = new MembershipController(sdk, this._tracer);

                        bool renewed = mc.RenewMembership(renewal);
                        this._tracer.Trace("mc.RenewMembership={0}", renewed);

                        if (!renewed)
                        {
                            throw new Exception("Unable to renew the membership, this might be because the membership is in an invalid state.");
                        }
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
                    throw new Exception("Invalid input. The Member cannot be null for a Membership");
                }
            }
            catch (FaultException<OrganizationServiceFault> fe)
            {
                if (fe.Detail != null)
                {
                    this._tracer.Trace(fe.Detail.ToString());
                }

                this._tracer.Trace(fe.ToString());

                string reference = renewal.MembershipNumber;
                throw new WebFaultException<Error>(ConvertToError(fe, reference), HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                this._tracer.Trace(ex.ToString());

                string reference = renewal.MembershipNumber;
                throw new WebFaultException<Error>(ConvertToError(ex, reference), HttpStatusCode.InternalServerError);
            }
            finally
            {
                // write to the log file
                this._tracer.WriteToLog(this._logPath);
            }
        }

        // This method has been replaced by RenewMembership (above), to handle the entire membership type, instead of just the amount
        public void Renew(Renewal renewal)
        {
            this._tracer.Trace("Method: MembershipService.Renew: MembershipNumber={0}, Amount={1}", renewal.MembershipNumber, renewal.Amount);

            try
            {
                if (!string.IsNullOrEmpty(renewal.MembershipNumber))
                {
                    var sdk = ConnectionController.ConnectToCrm(this._tracer);
                    if (sdk != null)
                    {
                        MembershipController mc = new MembershipController(sdk, this._tracer);

                        bool renewed = mc.RenewMembership(renewal);
                        this._tracer.Trace("mc.RenewMembership={0}", renewed);

                        if (!renewed)
                        {
                            throw new Exception("Unable to renew the membership, this might be because the membership is in an invalid state.");
                        }
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
                    throw new Exception("Invalid input. The Member cannot be null for a Membership");
                }
            }
            catch (FaultException<OrganizationServiceFault> fe)
            {
                if (fe.Detail != null)
                {
                    this._tracer.Trace(fe.Detail.ToString());
                }

                this._tracer.Trace(fe.ToString());

                string reference = renewal.MembershipNumber;
                throw new WebFaultException<Error>(ConvertToError(fe, reference), HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                this._tracer.Trace(ex.ToString());

                string reference = renewal.MembershipNumber;
                throw new WebFaultException<Error>(ConvertToError(ex, reference), HttpStatusCode.InternalServerError);
            }
            finally
            {
                // write to the log file
                this._tracer.WriteToLog(this._logPath);
            }
        }
        #endregion

        #region UPDATE
        /// <summary>
        /// Updates the details of an existing Membership
        /// </summary>
        public void Update(Membership membership)
        {
            this._tracer.Trace("Method: MembershipService.Update");

            try
            {
                this.LogMembershipInfo(membership);

                if (this.IsValidInput(membership))
                {
                    var sdk = ConnectionController.ConnectToCrm(this._tracer);
                    if (sdk != null)
                    {
                        MembershipController mc = new MembershipController(sdk, this._tracer);
                        mc.ProcessUpdate(membership);
                        this._tracer.Trace("Membership processed");
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
                    throw new Exception("Invalid input. The Member cannot be null for a Membership");
                }
            }
            catch (FaultException<OrganizationServiceFault> fe)
            {
                if (fe.Detail != null)
                {
                    this._tracer.Trace(fe.Detail.ToString());
                }

                this._tracer.Trace(fe.ToString());

                string reference = membership != null ? membership.DpsTransactionReference : "Membership is Null";
                throw new WebFaultException<Error>(ConvertToError(fe, reference), HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                this._tracer.Trace(ex.ToString());

                string reference = membership != null ? membership.DpsTransactionReference : "Membership is Null";
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
        private bool IsValidInput(Membership membership)
        {
            // every membership requires a Member and a Membership Type
            bool isValid = false;

            if (membership != null)
            {
                if (membership.Member != null && membership.MembershipType != null)
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

        private void LogMembershipInfo(Membership membership)
        {
            JavaScriptSerializer js = new JavaScriptSerializer();
            string rawData = js.Serialize(membership);
            _tracer.Trace("{0}", rawData);
        }
        #endregion
    }
}
