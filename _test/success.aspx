<%@ Page Language="C#" AutoEventWireup="false" EnableEventValidation="false" CodeBehind="success.aspx.cs" Inherits="Coastguard.Web.API._test.success" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Coastguard Donations - Test Page</title>
    <script type="text/javascript" src="common/js/jquery-1.6.2.min.js"></script>
    <script type="text/javascript" src="common/js/json2.js"></script>
    <script type="text/javascript" src="common/js/script.js"></script>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <div>
                <div class="form_content" align="left" style="width: 650px;" id="main">
                    <h2>TEST: Donations Page</h2>
                    Please fill out the form below and click 'submit'.<br />
                    <table border="0" width="650px" border="0" cellpadding="5" cellspacing="0" bgcolor="#e5e5e5"
                        class="Form_content">
                        <tr>
                            <td colspan="2">Contact Details
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <table border="0">
                                    <tr>
                                        <td colspan="2">
                                            <label>Title</label><br />
                                            <asp:DropDownList ID="titles" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>First Name</label><br />
                                            <asp:TextBox ID="firstname" runat="server" Enabled="false" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Last Name</label><br />
                                            <asp:TextBox ID="lastname" runat="server" Enabled="false" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Name on Tax Receipt</label><br />
                                            <asp:TextBox ID="nameontaxreceipts" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Date of Birth</label><br />
                                            <asp:TextBox ID="dateofbirth" runat="server" />
                                            dd/MM/yyyy
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Home Phone</label><br />
                                            <asp:TextBox ID="homephone" runat="server" />
                                        </td>
                                    </tr>
                                </table>
                            </td>
                            <td style="vertical-align: top">
                                <table border="0">
                                    <tr>
                                        <td>
                                            <label>Mobile Phone</label><br />
                                            <asp:TextBox ID="mobilephone" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>E-mail Address</label><br />
                                            <asp:TextBox ID="emailaddress" runat="server" Enabled="false" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Street</label><br />
                                            <asp:TextBox ID="street" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Suburb</label><br />
                                            <asp:TextBox ID="suburb" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>City</label><br />
                                            <asp:TextBox ID="city" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Postal Code</label><br />
                                            <asp:TextBox ID="postalcode" runat="server" />
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2">
                                <hr />
                                Donation Details
                            </td>
                        </tr>

                        <tr>
                            <td>
                                <table>
                                    <tr>
                                        <td>
                                            <label>DPS Response Text</label><br />
                                            <asp:TextBox ID="dpsresponse" runat="server" Enabled="false" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Amount</label><br />
                                            <asp:TextBox ID="amount" runat="server" Enabled="false" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Is Regular Gift?</label><br />
                                            <asp:CheckBox ID="isregulargift" runat="server" Enabled="false" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>DPS Billing ID</label><br />
                                            <asp:TextBox ID="dpsbillingid" runat="server" Enabled="false" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Transaction Reference #</label><br />
                                            <asp:TextBox ID="transactionreference" runat="server" Enabled="false" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Start Date</label><br />
                                            <asp:TextBox ID="startdate" runat="server" Enabled="false" />
                                            dd/MM/yyyy
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>End Date</label><br />
                                            <asp:TextBox ID="enddate" runat="server" Enabled="false" />
                                            dd/MM/yyyy
                                        </td>
                                    </tr>
                                </table>
                            </td>
                            <td style="vertical-align: top">
                                <table>
                                    <tr>
                                        <td>
                                            <label>Payment Frequency</label><br />
                                            <asp:DropDownList ID="frequencies" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>CC Expiry Date</label><br />
                                            <asp:TextBox ID="ccexpirydate" runat="server" Enabled="false" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Campaign</label><br />
                                            <asp:DropDownList ID="campaigns" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <label>Reason for Helping</label><br />
                                            <asp:DropDownList ID="reasonsforhelping" runat="server" />
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>

                        <tr>
                            <td colspan="2">
                                <label>Comments</label><br />
                                <asp:TextBox ID="comments" runat="server" Rows="3" MaxLength="2000" TextMode="MultiLine" Width="90%" />
                            </td>
                        </tr>

                        <tr>
                            <td valign="top">
                                <asp:Button ID="submit" Text="Submit" runat="server" OnClick="submit_Click" />
                            </td>
                            <td>&nbsp;
                            </td>
                        </tr>
                    </table>
                </div>
                <span id="thankyou" style="display: none;">
                    <h2>Thank You! We will be in touch with you shortly.</h2>
                </span>
                <div id="#result">
                </div>
            </div>
    </form>
</body>
</html>

