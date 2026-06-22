using BengalTex.ERP.Application.Common.Interfaces;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Default face-match implementation — does nothing (returns NotChecked). The selfie is still
/// captured and stored for supervisor review. Swap this for a real AI provider later without
/// touching the attendance commands.
/// </summary>
public sealed class NoOpFaceMatchService : IFaceMatchService
{
    public Task<FaceMatchOutcome> CompareAsync(string selfieStoragePath, string? referencePhotoPath, CancellationToken ct = default)
        => Task.FromResult(new FaceMatchOutcome(null, "NotChecked"));
}
