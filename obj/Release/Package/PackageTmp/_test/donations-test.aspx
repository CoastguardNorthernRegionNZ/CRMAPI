<%@ Page Language="C#" AutoEventWireup="false" CodeBehind="donations-test.aspx.cs" Inherits="Coastguard.Web.API._test.donations_test"  %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Test Form</title>
    <style type="text/css">
        body, table, td {
            font-family: Segoe UI;
            font-size: 10pt;
        }
    </style>
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
                            <td>Donation Details
                                <hr />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <label>Amount</label><br />
                                <asp:TextBox ID="amount" runat="server" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <label>Email Address</label><br />
                                <asp:TextBox ID="emailaddress" runat="server" Width="250px" /></td>
                        </tr>
                        <tr>
                            <td>
                                <label>Is this a regular gift?</label><br />
                                <asp:CheckBox ID="isregulargift" runat="server" />
                            </td>
                        </tr>

                        <tr>
                            <td>
                                <label>Start Date</label><br />
                                <asp:TextBox ID="startdate" runat="server" />
                                dd/MM/yyyy
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <label>End Date</label><br />
                                <asp:TextBox ID="enddate" runat="server" />
                                dd/MM/yyyy
                            </td>
                        </tr>

                        <tr>
                            <td valign="top">
                                <asp:Button ID="submit" Text="Submit" OnClick="submit_Click" runat="server" />
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
        </div>
    </form>
</body>
</html>
