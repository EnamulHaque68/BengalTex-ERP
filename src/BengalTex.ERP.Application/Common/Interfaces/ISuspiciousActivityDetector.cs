using BengalTex.ERP.Domain.ValueObjects;

namespace BengalTex.ERP.Application.Common.Interfaces;

public interface ISuspiciousActivityDetector
{
    Task<SuspicionAssessment> AssessLoginAsync(LoginContext context, CancellationToken ct = default);
}

public record LoginContext(
    string UserName,
    Guid? UserId,
    string DeviceFingerprintHash,
    string? UserAgent,
    string? IpAddress,
    GeoLocation? Location);

public record SuspicionAssessment(
    bool IsSuspicious,
    bool ShouldBlock,
    List<string> Reasons,
    int RiskScore);     // 0-100