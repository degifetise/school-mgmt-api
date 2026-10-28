using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using HaladeHighSchool.Api.Configuration;
using Microsoft.Extensions.Options;

namespace HaladeHighSchool.Api.Services;

public sealed class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ResendSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        HttpClient httpClient,
        IOptions<ResendSettings> settings,
        ILogger<EmailService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task SendStudentApprovalEmailAsync(
        string toEmail,
        string fullName,
        string generatedSchoolEmail,
        string tempPassword,
        CancellationToken cancellationToken = default) =>
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
            "Sign in with these credentials and change your password after signing in.",
            cancellationToken);

    public Task SendTeacherApprovalEmailAsync(
        string toEmail,
        string fullName,
        string registeredPassword,
        CancellationToken cancellationToken = default) =>
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
            "Sign in with these credentials and change your password after signing in.",
            cancellationToken);

    public Task SendPasswordResetApprovedEmailAsync(
        string toEmail,
        string fullName,
        string temporaryPassword,
        string loginUrl,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            toEmail,
            "Your school password reset was approved",
            $"<p>Hello {WebUtility.HtmlEncode(fullName)},</p>" +
            "<p>Your password reset request has been approved.</p>" +
            $"<p><strong>Temporary password:</strong> {WebUtility.HtmlEncode(temporaryPassword)}</p>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(loginUrl)}\">Sign in to your school account</a></p>" +
            "<p>You will be asked to change this temporary password after signing in.</p>",
            $"Hello {fullName},\n\n" +
            "Your password reset request has been approved.\n\n" +
            $"Temporary password: {temporaryPassword}\n" +
            $"Sign in: {loginUrl}\n\n" +
            "You will be asked to change this temporary password after signing in.",
            cancellationToken);

    private async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                throw new InvalidOperationException(
                    "Resend is not configured. Set Resend:ApiKey or EmailSettings:ApiKey.");
            }

            if (string.IsNullOrWhiteSpace(_settings.FromAddress))
            {
                throw new InvalidOperationException(
                    "The Resend sender address is not configured. Set EmailSettings:SenderEmail.");
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                throw new ArgumentException("A recipient email address is required.", nameof(toEmail));
            }

            var sender = string.IsNullOrWhiteSpace(_settings.FromName)
                ? _settings.FromAddress.Trim()
                : $"{_settings.FromName.Trim()} <{_settings.FromAddress.Trim()}>";
            using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
            {
                Headers =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey)
                },
                Content = JsonContent.Create(new ResendEmailRequest(
                    sender,
                    toEmail.Trim(),
                    subject,
                    htmlBody,
                    textBody))
            };

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Resend rejected email with status {(int)response.StatusCode} " +
                    $"({response.StatusCode}): {responseBody}",
                    inner: null,
                    statusCode: response.StatusCode);
            }
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Resend email delivery failed for recipient {Recipient}. Error: {ErrorMessage}. " +
                "Exception chain: {ExceptionDetails}",
                MaskEmailAddress(toEmail ?? string.Empty),
                exception.Message,
                exception.ToString());

            throw;
        }
    }

    private sealed record ResendEmailRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html,
        [property: JsonPropertyName("text")] string Text);

    private static string MaskEmailAddress(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex <= 1
            ? "[redacted]"
            : $"{email[0]}***{email[atIndex..]}";
    }
}
