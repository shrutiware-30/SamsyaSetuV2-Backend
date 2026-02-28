using System.Net;
using System.Net.Mail;

namespace G2CCRMPortal.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendWelcomeAsync(string toEmail, string name)
    {
        var subject = "Welcome to G2C CRM Portal!";
        var body = $"Hello {name},\n\nYour account has been created successfully.";
        await SendAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetAsync(string toEmail, string name, string resetToken)
    {
        var resetUrl = $"{_config["App:ClientUrl"]}/reset-password?token={resetToken}";
        var subject = "Password Reset Request (valid for 10 minutes)";
        var body = $"Hello {name},\n\nReset your password here: {resetUrl}\n\nIf you didn't request this, please ignore.";
        await SendAsync(toEmail, subject, body);
    }

    public async Task SendEscalationAsync(string toEmail, string issueId, string wardName)
    {
        var subject = $"SLA Breach Alert — Issue {issueId}";
        var body = $"Issue {issueId} in ward {wardName} has breached its SLA deadline.";
        await SendAsync(toEmail, subject, body);
    }

    public async Task SendOtpAsync(string toEmail, string otp)
    {
        var subject = "Your G2C CRM Verification Code";
        var body = $"Your verification code is {otp}.\n\nThis code is valid for 5 minutes.";
        await SendAsync(toEmail, subject, body);
    }

    private async Task SendAsync(string to, string subject, string body)
    {
        // TODO: Re-enable when SMTP credentials are configured
        using var smtp = new SmtpClient(_config["Email:Host"],
            int.Parse(_config["Email:Port"]!));
        smtp.Credentials = new NetworkCredential(
            _config["Email:Username"], _config["Email:Password"]);
        smtp.EnableSsl = true;

        var message = new MailMessage(_config["Email:From"]!, to, subject, body);
        await smtp.SendMailAsync(message);
        //await Task.CompletedTask;
    }
}