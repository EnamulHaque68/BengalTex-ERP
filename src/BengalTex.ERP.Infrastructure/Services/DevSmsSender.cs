using BengalTex.ERP.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Development SMS sender — logs to Serilog instead of dispatching a real SMS.
/// Lets developers verify the message body during local testing without
/// burning real SMS credits. Replace registration with SslWireless / Twilio
/// adapter in production.
/// </summary>
public class DevSmsSender : ISmsSender
{
    private readonly ILogger<DevSmsSender> _logger;

    public DevSmsSender(ILogger<DevSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[DEV SMS] To: {Phone} | Message: {Message}",
            phoneNumber, message);
        return Task.CompletedTask;
    }
}
