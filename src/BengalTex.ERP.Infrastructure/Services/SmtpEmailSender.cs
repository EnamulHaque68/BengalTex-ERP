using System.Net;
using System.Net.Mail;
using BengalTex.ERP.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Production email sender over SMTP (System.Net.Mail). Registered when
/// <c>Email:Provider = "Smtp"</c>; configured via <see cref="EmailSettings"/>
/// (Host/Port/UseSsl/Username/Password/From…). Otherwise <see cref="DevEmailSender"/>
/// logs instead of sending.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;

    public SmtpEmailSender(IOptions<EmailSettings> settings) => _settings = settings.Value;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        => SendAsync(new[] { to }, subject, htmlBody, ct);

    public async Task SendAsync(IEnumerable<string> toAddresses, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
            throw new InvalidOperationException("Email:Host is not configured for the SMTP sender.");

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        foreach (var to in toAddresses)
            message.To.Add(to);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl
        };
        if (!string.IsNullOrEmpty(_settings.Username))
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);

        await client.SendMailAsync(message, ct);
    }
}
