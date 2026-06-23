using BengalTex.ERP.Application.Common.Interfaces;

namespace BengalTex.ERP.Application.Company;

/// <summary>
/// Loads the company logo image bytes from storage (best-effort) so PDF renderers can stamp it on
/// invoices / statements / reports. Returns null when there's no logo or it can't be read — the
/// document still renders, just without the logo.
/// </summary>
public static class CompanyLogoLoader
{
    public static async Task<byte[]?> LoadAsync(string? logoPath, IFileStorage files, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(logoPath)) return null;
        try
        {
            if (!await files.ExistsAsync(logoPath, ct)) return null;
            await using var s = await files.OpenReadAsync(logoPath, ct);
            using var ms = new MemoryStream();
            await s.CopyToAsync(ms, ct);
            return ms.Length > 0 ? ms.ToArray() : null;
        }
        catch { return null; }
    }
}
