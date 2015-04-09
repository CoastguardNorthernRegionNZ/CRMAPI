using PaymentExpress.PxPay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Coastguard.Web.API._test
{
    public partial class donations_test : System.Web.UI.Page
    {
        protected override void OnInit(EventArgs e)
        {
            this.Load += Page_Load;
            base.OnInit(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
            }
        }

        protected void submit_Click(object sender, EventArgs e)
        {
            PxPay ws = new PxPay("Magnetism_Dev", "c21aa727d509e3828e79a21ab4f7a4b609b758817d83e87b7f3c722d7a88cd3a");
            RequestInput input = new RequestInput();

            decimal amount = 0;
            if (decimal.TryParse(this.amount.Text, out amount) && amount > 0)
            {
                bool isRegularGift = this.isregulargift.Checked;
                DateTime startDate = ParseDateTime(this.startdate.Text);

                input.AmountInput = string.Format("{0:0.00}", amount);
                input.CurrencyInput = "NZD";
                input.EmailAddress = this.emailaddress.Text;
                input.MerchantReference = Guid.NewGuid().ToString();
                input.TxnType = isRegularGift ? "Auth" : "Purchase";    // for all regular gifts, authorize (hold) the card. for one-off donations, send a purchase message
                input.EnableAddBillCard = this.isregulargift.Checked ? "1" : "0";
                input.TxnData1 = this.startdate.Text;
                input.TxnData2 = this.enddate.Text;
                input.UrlFail = string.Format("http://{0}/_test/fail.aspx", Request.Url.Authority);
                input.UrlSuccess = string.Format("http://{0}/_test/success.aspx", Request.Url.Authority);

                RequestOutput output = ws.GenerateRequest(input);
                if (output.valid == "1")
                {
                    Response.Redirect(output.Url);
                }
            }
        }

        private DateTime ParseDateTime(string input)
        {
            DateTime date = default(DateTime);
            DateTime.TryParse(input, out date);

            return date;
        }
    }
}