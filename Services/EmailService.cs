using System.Net;
using System.Net.Mail;

namespace G2CCRMPortal.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendWelcomeAsync(string toEmail, string name)
    {
        var subject = "Welcome to G2C CRM Portal!";
        var body = $"""
            <h1>Welcome, {name}!</h1>
            <p>Your account has been created successfully.</p>
            <p>You can now log in and start using the portal.</p>
            """;
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetAsync(string toEmail, string name, string resetToken)
    {
        var resetUrl = $"{_config["App:ClientUrl"]}/reset-password?token={resetToken}";
        var subject = "Password Reset Request (valid for 10 minutes)";
        var body = $"Hello {name},\n\nReset your password here: {resetUrl}\n\nIf you didn't request this, please ignore.";
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendEscalationAsync(string toEmail, string issueId, string wardName)
    {
        var subject = $"Issue Escalation Alert - {wardName}";
        var body = $"""
            <h2>⚠️ Issue Escalation Notice</h2>
            <p>An issue (ID: {issueId}) in {wardName} ward has been escalated.</p>
            <p>Please take immediate action.</p>
            <a href="http://localhost:5173/admin/issues/{issueId}">View Issue</a>
            """;
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendOtpAsync(string toEmail, string otp)
    {
        var subject = "Your OTP for G2C CRM Portal";
        var body = $"""
            <h1>Your OTP</h1>
            <p>Your one-time password is: <strong>{otp}</strong></p>
            <p>This OTP is valid for 5 minutes.</p>
            <p>Do not share this with anyone.</p>
            """;
        await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Sends SLA breach escalation email to department head/ward officer.
    /// </summary>
    public async Task SendSlaEscalationAsync(string toEmail, string issueId, string wardName,
        string description, string citizenName, int hoursOverdue)
    {
        var subject = $"🔴 URGENT: SLA Breached - {wardName} Ward";
        var body = $"""
            <html>
            <body style="font-family: Arial, sans-serif;">
                <div style="background-color: #ffebee; padding: 20px; border-left: 5px solid #d32f2f;">
                    <h2 style="color: #d32f2f;">⚠️ SLA BREACH ALERT</h2>
                    <p><strong>Ward:</strong> {wardName}</p>
                    <p><strong>Issue ID:</strong> {issueId}</p>
                    <p><strong>Citizen Name:</strong> {citizenName}</p>
                    <p><strong>Issue Description:</strong> {description}</p>
                    <p><strong style="color: #d32f2f;">Hours Overdue:</strong> {hoursOverdue} hours</p>
                    <p>This complaint has exceeded its Service Level Agreement (SLA) deadline and requires immediate attention.</p>
                    <a href="http://localhost:5173/admin/issues/{issueId}"
                       style="background-color: #d32f2f; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;">
                        View Issue & Take Action
                    </a>
                    <p style="margin-top: 20px; font-size: 12px; color: #666;">
                        This is an automated alert. Please do not reply to this email.
                    </p>
                </div>
            </body>
            </html>
            """;
        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var smtpHost = _config["Email:Host"]!;
            var smtpPort = int.Parse(_config["Email:Port"]!);
            var fromEmail = _config["Email:From"]!;
            var username = _config["Email:Username"]!;
            var password = _config["Email:Password"]!;

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(username, password);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent to {toEmail}: {subject}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending email to {toEmail}: {ex.Message}");
            throw;
        }
    }
}