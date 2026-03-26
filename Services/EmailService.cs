using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Threading;


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
        var subject = "Welcome to Samasya Setu : Citizen Grievance Redressal Portal!";
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
        var body = $$"""
            <h1>Welcome to Samasya Setu : Citizen Grievance Redressal Portal!<h1>
            <p>Hello {{name}},\n\nReset your password here: {{resetUrl}}\n\nIf you didn't request this, please ignore.</p>
            """;
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendEscalationAsync(string toEmail, string issueId, string wardName)
    {{
        var subject = $"Issue Escalation Alert - {wardName}";
        var body = $"""
            <h2>⚠️ Issue Escalation Notice</h2>
            <p>An issue (ID: {issueId}) in {wardName} ward has been escalated.</p>
            <p>Please take immediate action.</p>
            <a href="http://localhost:5173/admin/issues/{issueId}">View Issue</a>
            """;
        await SendEmailAsync(toEmail, subject, body);
    }}

    public async Task SendOtpAsync(string toEmail, string otp)
    {
        {
            var subject = "Your OTP for Samasya Setu : Citizen Grievance Redressal Portal!";
            var body = $"""
            <h1>Your OTP</h1>
            <p>Your one-time password is: <strong>{otp}</strong></p>
            <p>This OTP is valid for 5 minutes.</p>
            <p>Do not share this with anyone.</p>
            """;
            await SendEmailAsync(toEmail, subject, body);
        }
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

    public async Task SendIssueClosedAsync(
        string toEmail,
        string citizenName,
        string issueName,
        string wardName,
        DateTime createdAt,
        Guid issueId)
    {
        var subject = "Citizen Issue Resolved and Closed";

        var feedbackUrl = $"{_config["App:ClientUrl"]}/issues/{issueId}";

        var body = $"""
        <html>
        <body style="font-family: Arial, sans-serif; color: #333;">
            <p>Dear {citizenName},</p>

            <p>
                This is to inform you that the following citizen issue has been
                <strong>successfully resolved and closed</strong>.
            </p>

            <table cellpadding="6" cellspacing="0">
                <tr>
                    <td><strong>Issue:</strong></td>
                    <td>{issueName}</td>
                </tr>
                <tr>
                    <td><strong>Ward:</strong></td>
                    <td>{wardName}</td>
                </tr>
                <tr>
                    <td><strong>Raised At:</strong></td>
                    <td>{createdAt:dd MMM yyyy, hh:mm tt}</td>
                </tr>
            </table>

            <p style="margin-top:16px;">
                Kindly submit your feedback by visiting our portal.
                Your feedback is valuable and helps us improve our services.
            </p>

            <p>
                <a href="{feedbackUrl}"
                   style="background-color:#1976d2;color:white;
                          padding:10px 16px;text-decoration:none;
                          border-radius:4px;">
                    Submit Feedback
                </a>
            </p>

            <p style="margin-top:20px;">
                Thank you for your cooperation.
            </p>

            <p>
                Regards,<br/>
                <strong>Samasya Setu Portal Team</strong>
            </p>

            <p style="font-size:12px;color:#777;">
                This is an automated notification. Please do not reply.
            </p>
        </body>
        </html>
        """;

        await SendEmailAsync(toEmail, subject, body);
    }


    private async Task SendEmailAsync(string toEmail, string subject, string bodyHtml, CancellationToken ct = default)
{
    var smtpHost = _config["Email:Host"]!;            // e.g., smtp.gmail.com
    var smtpPort = int.Parse(_config["Email:Port"]!); // 587 or 465
    var fromEmail = _config["Email:From"]!;
    var username = _config["Email:Username"]!;       // full Gmail address
    var password = _config["Email:Password"]!;       // 16-char App Password (not normal pwd)

    // Build the message
    var message = new MimeMessage();
    message.From.Add(MailboxAddress.Parse(fromEmail));
    message.To.Add(MailboxAddress.Parse(toEmail));
    message.Subject = subject ?? string.Empty;

    var builder = new BodyBuilder { HtmlBody = bodyHtml, TextBody = StripHtml(bodyHtml) };
    message.Body = builder.ToMessageBody();

    using var client = new SmtpClient (); // 15s total I/O timeout

    // Choose TLS mode based on port
    var secure = smtpPort == 465 ? SecureSocketOptions.SslOnConnect
                                 : SecureSocketOptions.StartTls;

    try
    {
        // 1) Connect
        await client.ConnectAsync(smtpHost, smtpPort, secure, ct);

        // 2) If you’re using App Passwords (not OAuth), remove XOAUTH2
        client.AuthenticationMechanisms.Remove("XOAUTH2");

        // 3) Authenticate
        await client.AuthenticateAsync(username, password, ct);

        // 4) Send
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
        _logger.LogInformation("Email sent to {To}: {Subject}", toEmail, subject);
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        _logger.LogError("Email send canceled for {To}", toEmail);
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending email to {To}", toEmail);
        throw;
    }
}

private static string StripHtml(string html) =>
    string.IsNullOrWhiteSpace(html) ? string.Empty
    : System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ");
}