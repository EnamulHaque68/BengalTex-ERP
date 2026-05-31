namespace BengalTex.ERP.Application.Services;

/// <summary>
/// Generates QR code PNG bytes for a given payload. Used by JobCards to print
/// scannable cards (and any future polymorphic QR uses — Sample tag, Stock tag, etc.).
/// </summary>
public interface IQrCodeService
{
    /// <summary>Returns raw PNG bytes encoding <paramref name="payload"/>.</summary>
    /// <param name="payload">String content the QR encodes (e.g. a job-card code).</param>
    /// <param name="pixelsPerModule">QR module size (≈ resolution). Default 8 → ~240x240 px.</param>
    byte[] GeneratePng(string payload, int pixelsPerModule = 8);
}
