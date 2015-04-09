
using Coastguard.Web.API.Models;
using Coastguard.Web.API.Services;
using PaymentExpress.PxPay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Coastguard.Web.API._test
{
    public partial class success : System.Web.UI.Page
    {
        protected override void OnInit(EventArgs e)
        {
            this.Load += Page_Load;
            base.OnInit(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!this.IsPostBack)
            {
                string result = Request.QueryString["result"];
                if (!string.IsNullOrEmpty(result))
                {
                    ProcessDpsPayment(result);
                }

                PrefillDropdownLists();
                if (!this.isregulargift.Checked)
                {
                    this.frequencies.SelectedIndex = 5; // one off
                    this.frequencies.Enabled = false;
                }
            }
        }

        private void ProcessDpsPayment(string result)
        {
            PxPay ws = new PxPay("Magnetism_Dev", "c21aa727d509e3828e79a21ab4f7a4b609b758817d83e87b7f3c722d7a88cd3a");
            ResponseOutput output = ws.ProcessResponse(result);

            string[] nameSplit = output.CardHolderName.Split(' ');
            string firstName = nameSplit[0];
            string lastName = string.Join(" ", nameSplit.Skip(1).ToList());

            this.amount.Text = output.AmountSettlement;
            this.emailaddress.Text = output.EmailAddress;
            this.firstname.Text = firstName;
            this.lastname.Text = lastName;
            this.dpsbillingid.Text = output.DpsBillingId;
            this.transactionreference.Text = output.DpsTxnRef;
            this.ccexpirydate.Text = output.DateExpiry;
            this.dpsresponse.Text = output.ResponseText;
            this.startdate.Text = output.TxnData1;
            this.enddate.Text = output.TxnData2;
            this.isregulargift.Checked = !string.IsNullOrEmpty(this.dpsbillingid.Text); // if the billing id contains data, it's a regular gift
        }

        private void PrefillDropdownLists()
        {
            DonationService service = new DonationService();
            PageData data = service.GetPageData("CNZ");

            if (data != null)
            {
                this.titles.DataSource = data.Salutations;
                this.titles.DataTextField = "Value";
                this.titles.DataValueField = "Key";
                this.titles.DataBind();

                this.campaigns.DataSource = data.Campaigns;
                this.campaigns.DataTextField = "Name";
                this.campaigns.DataValueField = "CampaignId";
                this.campaigns.DataBind();

                this.frequencies.DataSource = data.PaymentFrequencies;
                this.frequencies.DataTextField = "Value";
                this.frequencies.DataValueField = "Key";
                this.frequencies.DataBind();

                this.reasonsforhelping.DataSource = data.ReasonsForHelping;
                this.reasonsforhelping.DataTextField = "Name";
                this.reasonsforhelping.DataValueField = "ReasonForHelpingId";
                this.reasonsforhelping.DataBind();
            }
        }

        protected void submit_Click(object sender, EventArgs e)
        {
            Donation d = new Donation
            {
                Donor = new Contact
                {
                    FirstName = this.firstname.Text,
                    LastName = this.lastname.Text,
                    NameOnTaxReceipt = this.nameontaxreceipts.Text,
                    HomePhone = this.homephone.Text,
                    MobilePhone = this.mobilephone.Text,
                    EmailAddress = this.emailaddress.Text,
                    Street = this.street.Text,
                    Suburb = this.suburb.Text,
                    City = this.city.Text,
                    PostalCode = this.postalcode.Text,
                    DateOfBirth = ParseDateTime(this.dateofbirth.Text),
                    SalutationCode = ParseInt(this.titles.SelectedValue),
                },
                IsRegularGift = this.isregulargift.Checked,
                DpsTransactionReference = this.transactionreference.Text,
                CCExpiry = this.ccexpirydate.Text,
                Comments = this.comments.Text,
                Amount = ParseDecimal(this.amount.Text),
                Date = DateTime.Now,
                DpsPaymentSuccessful = true,
                DpsResponseText = this.dpsresponse.Text,
                CampaignId = ParseGuid(this.campaigns.SelectedValue),
                ReasonForHelpingId = ParseGuid(this.reasonsforhelping.SelectedValue),
                RegionCode = "CNZ",
                Pledge = new Pledge
                {
                    DpsBillingId = this.dpsbillingid.Text,
                    PaymentFrequencyCode = ParseInt(this.frequencies.SelectedValue),
                    StartDate = ParseDateTime(this.startdate.Text),
                    EndDate = ParseDateTime(this.enddate.Text)
                }
            };

            DonationService service = new DonationService();
            service.PostDonation(d);

            Response.Redirect("/_test/donations-test.aspx");
        }

        private DateTime? ParseDateTime(string input)
        {
            DateTime? dt = null;
            DateTime parsed = default(DateTime);

            if (DateTime.TryParse(input, out parsed))
            {
                if (parsed != default(DateTime))
                {
                    dt = parsed;
                }
            }

            return dt;
        }

        private decimal ParseDecimal(string input)
        {
            decimal value = default(decimal);
            decimal.TryParse(input, out value);

            return value;
        }

        private int ParseInt(string input)
        {
            int value = default(int);
            int.TryParse(input, out value);

            return value;
        }

        private Guid ParseGuid(string input)
        {
            Guid id = default(Guid);
            Guid.TryParse(input, out id);

            return id;
        }
    }
}