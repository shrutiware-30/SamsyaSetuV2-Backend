namespace G2CCRMPortal.Services;

public interface IEmailService
{
    Task SendWelcomeAsync(string toEmail, string name);
    Task SendPasswordResetAsync(string toEmail, string name, string resetToken);
    Task SendEscalationAsync(string toEmail, string issueId, string wardName);
    Task SendOtpAsync(string toEmail, string otp);
}