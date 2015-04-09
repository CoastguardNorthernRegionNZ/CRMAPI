using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Coastguard.Web.API.Models
{
    [Flags]
    public enum ContactMatchCode : int
    {
        None = 0,
        Email = 1,
        FirstName = 2,
        LastName = 4,
        MobilePhone = 8,
        HomePhone = 16,
    }

    [Flags]
    public enum PledgeMatchCode : int
    {
        None = 0,
        DpsBillingId = 1,
        Contact = 2,
        Amount = 4
    }

    [Flags]
    public enum BatchMatchCode : int
    {
        None = 0,
        BatchType = 1,
        TransactionDate = 2,
        PaymentFrequency = 4,
        PaymentMethod = 8
    }

    [Flags]
    public enum MembershipMatchCode : int
    {
        None = 0,
        Contact = 1,
        MembershipNumber = 2,
        UnpaidInvoice = 4
    }

    public enum DonationForCode
    {
        Individual = 809700000,
        Organisation = 809700001
    }

    public enum DonationStatusCode
    {
        Outstanding = 1,
        Processing = 809700000,
        Collected = 809700001,
        Inactive = 2
    }

    public enum DonationTypeCode
    {
        Cash = 809700000,
    }

    public enum PaymentMethodCode
    {
        CreditCard = 809730005
    }

    public enum PaymentFrequencyCode
    {
        Weekly = 809730000,
        Monthly = 809730001,
        Quarterly = 809730002,
        Semiannually = 809730003,
        Annually = 809730004,
        OneOff = 809730005
    }

    public enum PaymentRepeatOptionCode
    {
        UntilFurtherNotice = 809730000,
        SetExpiryDate = 809730001,
        PaymentHoliday = 809730002
    }

    public enum PledgeStatusCode
    {
        Active = 1,
        Fulfilled = 809700000,
        Lapsed = 809700001
    }

    public enum BatchTypeCode
    {
        BankStatement = 809730000,
        CreditCard = 809730001,
        ManualReceipts = 809730002
    }

    public enum BatchStatusCode
    {
        Open = 809730000,
        Closed = 809730001,
        OnHold = 809730002
    }

    public enum PaymentTransactionTypeCode
    {
        Cash = 809730000,
        Cheque = 809730001,
        CreditCard = 809730002,
        DirectCredit = 809730003,
        DirectDebit = 809730004
    }

    public enum ProductTypeCode
    {
        Membership = 809730000
    }

    public enum MemberTypeCode
    {
        IndividualFamily = 809730000
    }

    public enum AvailableForCode
    {
        NewMembers = 809730000,
        Renewals = 809730001,
        AllMembers = 809730002
    }

    public enum TagAppliesToCode
    {
        Campaign = 809730000,
        CoastguardRegion = 809730001,
        CoastguardUnit = 809730002
    }

    public enum InvoiceStateCode
    {
        Active = 0,
        Paid = 2,
    }

    public enum InvoiceStatusCode
    {
        Complete = 100001
    }

    public enum GenderCode
    {
        Male = 1,
        Female = 2
    }

    public enum MembershipSourceCode
    {
        Online = 809730000
    }

    public enum MembershipStatusCode
    {
        PendingRenewal = 809730000
    }

    // note. This is used for unit tests only. We retrieve the option set metadata from crm and present it to the website via a "GetPageData" web method
    public enum SalutationCode
    {
        Mr = 809730000,
        Mrs,
        Miss,
        Master,
        Ms,
        Dr
    }
}
