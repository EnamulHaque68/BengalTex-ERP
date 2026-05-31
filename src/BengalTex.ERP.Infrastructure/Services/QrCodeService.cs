using BengalTex.ERP.Application.Services;
using QRCoder;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>QRCoder-backed implementation of <see cref="IQrCodeService"/>.</summary>
public sealed class QrCodeService : IQrCodeService
{
    public byte[] GeneratePng(string payload, int pixelsPerModule = 8)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload must not be empty.", nameof(payload));
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }
}
