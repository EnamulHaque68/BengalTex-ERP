using BengalTex.ERP.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Development email sender — logs to Serilog instead of dispatching a real email.
/// Lets developers verify the message body during local testing without an SMTP
/// configuration. Replace registration with SmtpEmailSender / SendGridEmailSender
/// in production.
/// </summary>
public class DevEmailSender : IEmailSender
{
    private readonly ILogger<DevEmailSender> _logger;

    public DevEmailSender(ILogger<DevEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL] To: {To} | Subject: {Subject} | Body: {Body}",
            to, subject, htmlBody);
        return Task.CompletedTask;
    }

    public Task SendAsync(IEnumerable<string> toAddresses, string subject, string htmlBody, CancellationToken ct = default)
        => SendAsync(toAddresses, subject, htmlBody, Array.Empty<EmailAttachment>(), ct);

    public Task SendAsync(
        IEnumerable<string> toAddresses,
        string subject,
        string htmlBody,
        IReadOnlyList<EmailAttachment> attachments,
        CancellationToken ct = default)
    {
        var attachmentSummary = attachments.Count == 0
            ? "(none)"
            : string.Join(", ", attachments.Select(a => $"{a.FileName} ({a.Content.Length} bytes)"));
        _logger.LogInformation(
            "[DEV EMAIL] To: {To} | Subject: {Subject} | Attachments: {Attachments} | Body: {Body}",
            string.Join(", ", toAddresses), subject, attachmentSummary, htmlBody);
        return Task.CompletedTask;
    }
}
