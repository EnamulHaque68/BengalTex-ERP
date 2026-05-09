namespace BengalTex.ERP.Application.Common.Interfaces;

public interface IDeviceFingerprintService
{
    /// <summary>
    /// Hashes the raw client-supplied fingerprint with a server-side salt.
    /// Client uses FingerprintJS (open-source) on the browser side.
    /// </summary>
    string HashFingerprint(string rawFingerprint);

    /// <summary>
    /// Composes a richer signature from multiple signals (raw fp + UA + screen + tz).
    /// </summary>
    string ComposeSignature(DeviceSignals signals);
}

public record DeviceSignals(
    string RawFingerprint,
    string? UserAgent,
    string? ScreenResolution,
    string? Timezone,
    string? Language,
    string? Platform);