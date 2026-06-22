namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>
/// Compares a check-in selfie against an employee's reference photo. Architecture hook for a
/// future AI face-match provider. The default Infrastructure impl is a no-op (returns NotChecked,
/// never blocks) — selfie is stored for supervisor review until a real provider is plugged in.
/// </summary>
public interface IFaceMatchService
{
    Task<FaceMatchOutcome> CompareAsync(string selfieStoragePath, string? referencePhotoPath, CancellationToken ct = default);
}

/// <summary>Score 0–100 (null = not evaluated). Status mirrors the AttendanceRecord.FaceMatchStatus enum names.</summary>
public record FaceMatchOutcome(decimal? Score, string Status);
