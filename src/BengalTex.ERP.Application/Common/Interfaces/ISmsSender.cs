namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>
/// SMS gateway abstraction. Dev: logs to Serilog (no real send).
/// Production: swap registration to SslWireless / Twilio adapter.
/// </summary>
public interface ISmsSender
{
    /// <summary>
    /// Sends an SMS. phoneNumber should be in international format (e.g., +8801XXXXXXXXX).
    /// </summary>
    Task SendAsync(string phoneNumber, string message, CancellationToken ct = default);
}
