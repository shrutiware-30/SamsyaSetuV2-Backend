namespace G2CCRMPortal.Services;

public interface IEmailService
{
    Task SendWelcomeAsync(string toEmail, string name);
    Task SendPasswordResetAsync(string toEmail, string name, string resetToken);
    Task SendEscalationAsync(string toEmail, string issueId, string wardName);
    Task SendOtpAsync(string toEmail, string otp);
    Task SendSlaEscalationAsync(string toEmail, string issueId, string wardName, string description, string citizenName, int hoursOverdue);
    Task SendIssueClosedAsync(
    string toEmail,
    string citizenName,
    string issueName,
    string wardName,
    DateTime createdAt,
    Guid issueId
);
}