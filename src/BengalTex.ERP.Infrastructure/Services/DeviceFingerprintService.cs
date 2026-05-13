using System.Security.Cryptography;
using System.Text;
using BengalTex.ERP.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Infrastructure.Services;

public class DeviceFingerprintService : IDeviceFingerprintService
{
    private readonly byte[] _saltBytes;

    public DeviceFingerprintService(IOptions<DeviceFingerprintSettings> settings)
    {
        _saltBytes = Convert.FromBase64String(settings.Value.Salt);
    }

    public string HashFingerprint(string rawFingerprint)
    {
        using var hmac = new HMACSHA256(_saltBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawFingerprint));
        return Convert.ToBase64String(hash);
    }

    public string ComposeSignature(DeviceSignals signals)
    {
        var composite = string.Join("|",
            signals.RawFingerprint,
            signals.UserAgent ?? string.Empty,
            signals.ScreenResolution ?? string.Empty,
            signals.Timezone ?? string.Empty,
            signals.Language ?? string.Empty,
            signals.Platform ?? string.Empty);

        return HashFingerprint(composite);
    }
}
