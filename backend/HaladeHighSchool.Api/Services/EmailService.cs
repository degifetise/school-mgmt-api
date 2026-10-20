using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HaladeHighSchool.Api.Services;

public sealed class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task SendStudentApprovalEmailAsync(
        string toEmail,
        string fullName,
        string generatedSchoolEmail,
        string tempPassword) =>
        SendAsync(
            toEmail,
            "Your school issued account is ready",
            $"<p>Hello {WebUtility.HtmlEncode(fullName)},</p>" +
            "<p>Your school issued account has been approved.</p>" +
            $"<p><strong>School email:</strong> {WebUtility.HtmlEncode(generatedSchoolEmail)}<br>" +
            $"<strong>Temporary password:</strong> {WebUtility.HtmlEncode(tempPassword)}</p>" +
            "<p>Sign in with these credentials and change your password after signing in.</p>",
            $"Hello {fullName},\n\n" +
            "Your school issued account has been approved.\n\n" +
            $"School email: {generatedSchoolEmail}\n" +
            $"Temporary password: {tempPassword}\n\n" +
            "Sign in with these credentials and change your password after signing in.");

    public Task SendTeacherApprovalEmailAsync(
        string toEmail,
        string fullName,
        string registeredPassword) =>
        SendAsync(
            toEmail,
            "Your school issued account is ready",
            $"<p>Hello {WebUtility.HtmlEncode(fullName)},</p>" +
            "<p>Your school issued account has been approved.</p>" +
            $"<p><strong>Sign-in email:</strong> {WebUtility.HtmlEncode(toEmail)}<br>" +
            $"<strong>Password:</strong> {WebUtility.HtmlEncode(registeredPassword)}</p>" +
            "<p>Sign in with these credentials and change your password after signing in.</p>",
            $"Hello {fullName},\n\n" +
            "Your school issued account has been approved.\n\n" +
            $"Sign-in email: {toEmail}\n" +
            $"Password: {registeredPassword}\n\n" +
            "Sign in with these credentials and change your password after signing in.");

    private async Task SendAsync(string toEmail, string subject, string htmlBody, string textBody)
    {
        var smtpServer = GetRequiredSetting("EmailSettings:SmtpServer");
        var senderEmail = GetRequiredSetting("EmailSettings:SenderEmail");
        var senderName = GetRequiredSetting("EmailSettings:SenderName");
        var password = GetRequiredSetting("EmailSettings:Password");

        if (!int.TryParse(GetRequiredSetting("EmailSettings:Port"), out var port))
        {
            throw new InvalidOperationException("Configuration key 'EmailSettings:Port' must be a valid port number.");
        }

        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("A recipient email address is required.", nameof(toEmail));
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody
        }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(senderEmail, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private string GetRequiredSetting(string key)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required configuration key '{key}' is missing.");
        }

        return value;
    }
}
