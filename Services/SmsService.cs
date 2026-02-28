using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace G2CCRMPortal.Services;

public class SmsService : ISmsService
{
    private readonly string _fromNumber;

    public SmsService(IConfiguration config)
    {
        var sid = config["Twilio:AccountSid"]!;
        var token = config["Twilio:AuthToken"]!;
        _fromNumber = config["Twilio:FromNumber"]!;

        TwilioClient.Init(sid, token);
    }

    public async Task SendOtpAsync(string mobileNumber, string otp)
    {
        // Ensure E.164 format
        if (!mobileNumber.StartsWith("+"))
            mobileNumber = "+91" + mobileNumber; // adjust default country code as needed

        await MessageResource.CreateAsync(
            to: new PhoneNumber(mobileNumber),
            from: new PhoneNumber(_fromNumber),
            body: $"Your G2C CRM verification code is {otp}. Valid for 5 minutes.");
    }
}