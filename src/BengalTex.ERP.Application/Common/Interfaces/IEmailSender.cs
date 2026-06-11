namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>One attachment to ride along with an outbound email.</summary>
public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Email gateway abstraction. Dev: logs to Serilog (no real send).
/// Production: swap registration to SMTP / SendGrid / SES adapter.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);

    Task SendAsync(IEnumerable<string> toAddresses, string subject, string htmlBody, CancellationToken ct = default);

    /// <summary>
    /// Send with file attachments. Attachments may be empty — semantically equivalent to the
    /// no-attachment overload. Each <see cref="EmailAttachment"/> rides along as a separate
    /// MIME part (PDF / image / etc.).
    /// </summary>
    Task SendAsync(
        IEnumerable<string> toAddresses,
        string subject,
        string htmlBody,
        IReadOnlyList<EmailAttachment> attachments,
        CancellationToken ct = default);
}
