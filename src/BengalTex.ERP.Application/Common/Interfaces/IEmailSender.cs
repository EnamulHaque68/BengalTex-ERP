namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>
/// Email gateway abstraction. Dev: logs to Serilog (no real send).
/// Production: swap registration to SMTP / SendGrid / SES adapter.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);

    Task SendAsync(IEnumerable<string> toAddresses, string subject, string htmlBody, CancellationToken ct = default);
}
