namespace G2CCRMPortal.Services;

public interface ISmsService
{
    Task SendOtpAsync(string mobileNumber, string otp);
}