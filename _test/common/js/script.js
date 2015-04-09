var wsUrl = "http://muffin.magnetism.co.nz:81/Service.svc";
//var wsUrl = "http://localhost:1812/Service.svc";

$.fn.serializeObject = function () {
    var o = {};
    var a = this.serializeArray();
    $.each(a, function () {
        if (o[this.name] !== undefined) {
            if (!o[this.name].push) {
                o[this.name] = [o[this.name]];
            }
            o[this.name].push(this.value || '');
        } else {
            o[this.name] = this.value || '';
        }
    });
    return o;


};

$(document).ready(function () {
    //$.ajax(
    //    {
    //        type: "GET",
    //        contentType: "application/json;charset=utf-8",
    //        url: wsUrl + "/page/cnz",
    //        dataType: "json",
    //        cache: false,
    //        success: function (result) {
    //            if (result != null) {
    //                var titles = result.Salutations;
    //                var campaigns = result.Campaigns;
    //                var reasons = result.ReasonsForHelping;
    //                var frequencies = result.PaymentFrequencies;

    //                BindTitles(titles);
    //                BindCampaigns(campaigns);
    //                BindReasonsForHelping(reasons);
    //                BindFrequencies(frequencies);
    //            }
    //        },
    //        error: function (e) {
    //            alert(e.responseText);
    //        }
    //    }
    //);
});

function BindTitles(titles) {
    var items = "";
    for (var i = 0; i < titles.length; i++) {
        var item = titles[i];

        items += "<option value='" + item.Key + "'>" + item.Value + "</option>";
    }

    $("#titles").html(items);
}

function BindCampaigns(campaigns) {
    var items = "";
    for (var i = 0; i < campaigns.length; i++) {
        var item = campaigns[i];

        items += "<option value='" + item.CampaignId + "'>" + item.Name + "</option>";
    }

    $("#campaigns").html(items);
}

function BindReasonsForHelping(reasons) {
    var items = "";
    for (var i = 0; i < reasons.length; i++) {
        var item = reasons[i];

        items += "<option value='" + item.ReasonForHelpingId + "'>" + item.Name + "</option>";
    }

    $("#reasonsforhelping").html(items);
}

function BindFrequencies(frequencies) {
    var items = "";
    for (var i = 0; i < frequencies.length; i++) {
        var item = frequencies[i];

        items += "<option value='" + item.Key + "'>" + item.Value + "</option>";
    }

    $("#frequencies").html(items);
}

function GetTxnType(isRegularGift) {
    if (isRegularGift) {
        type = "Auth";
    } else {
        type = "Purchase";
    }

    return type;
}

function SubmitToWCF(method) {

    $.post(wsUrl + "/donations",
    {
        Amount: 100
    },
    function (data, status) {
        alert("Data: " + data + "\nStatus: " + status);
    });

    //var donation = {};
    //donation.pledge = {};
    //donation.donor = {};

    //donation.Amount = 100;
    //donation.Date = new Date();
    //donation.IsRegularGift = true;
    //donation.DpsPaymentSuccessful = true;
    //donation.DpsResponseText = "The transaction was successful";
    //donation.DpsTransactionReference = "XXX123";
    //donation.CCExpiryDate = new Date();
    //donation.CampaignId = "";
    //donation.Comments = "No comment, I just want to donate but now I realise I have commented so this is my comment.";
    //donation.ReasonForHelpingId = "";

    //donation.pledge.PaymentFrequency = 809730000;
    //donation.pledge.StartDate = new Date();
    //donation.pledge.EndDate = new Date();

    //donation.donor.Title = 809730000;
    //donation.donor.FirstName = "Roshan";
    //donation.donor.LastName = "Mehta";
    //donation.donor.NameOnTaxReceipt = "Mr Sir Dr Roshan Mehta";
    //donation.donor.DateOfBirth = new Date();
    //donation.donor.Company = "Magnetism";
    //donation.donor.HomePhone = "1234567";
    //donation.donor.MobilePhone = "021123456";
    //donation.donor.EmailAddress = "roshan@magnetismsolutions.com";
    //donation.donor.Street = "26 Greenpark Road";
    //donation.donor.Suburb = "Penrose";
    //donation.donor.City = "Auckland";
    //donation.donor.PostalCode = "1061";

    //donation.Amount = $("#amount").val();
    //donation.Date = $("#donationdate").val();
    //donation.IsRegularGift = $("#isregulargift").val();
    //donation.DpsPaymentSuccessful = $("#dpspaymentsuccessful").val();
    //donation.DpsResponseText = $("#dpsresponsetext").val();
    //donation.DpsTransactionReference = $("#dpstransactionreference").val();
    //donation.CCExpiryDate = $("#ccexpirydate").val();
    //donation.CampaignId = $("#campaigns").val();
    //donation.Comments = $("#comments").val();
    //donation.ReasonForHelpingId = $("#reasons").val();

    //donation.pledge.PaymentFrequency = $("#frequencies").val();
    //donation.pledge.StartDate = $("#startdate").val();
    //donation.pledge.EndDate = $("#enddate").val();

    //donation.donor.Title = $("#titles").val();
    //donation.donor.FirstName = $("#firstname").val();
    //donation.donor.LastName = $("#lastname").val();
    //donation.donor.NameOnTaxReceipt = $("#nameontaxreceipt").val();
    //donation.donor.DateOfBirth = $("#dateofbirth").val();
    //donation.donor.Company = $("#company").val();
    //donation.donor.HomePhone = $("#homephone").val();
    //donation.donor.MobilePhone = $("#mobilephone").val();
    //donation.donor.EmailAddress = $("#emailaddress").val();
    //donation.donor.Street = $("#street").val();
    //donation.donor.Suburb = $("#suburb").val();
    //donation.donor.City = $("#city").val();
    //donation.donor.PostalCode = $("#postcode").val();

    //var data = JSON.stringify(donation);

    //$.ajax(
    //        {
    //            type: "POST",
    //            contentType: "application/json;charset=utf-8",
    //            url: wsUrl + "/donations",
    //            data: data,
    //            dataType: "json",
    //            success: function(result) {
    //                alert("success");
    //            },
    //            error: function (e) {
    //                alert("error");
    //                alert(e.responseText);
    //            }
    //        });
}