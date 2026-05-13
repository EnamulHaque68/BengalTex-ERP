using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.ValueObjects;
using BengalTex.ERP.Infrastructure.Identity;
using BengalTex.ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

public class SuspiciousActivityDetector : ISuspiciousActivityDetector
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public SuspiciousActivityDetector(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<SuspicionAssessment> AssessLoginAsync(LoginContext context, CancellationToken ct = default)
    {
        var reasons = new List<string>();
        int riskScore = 0;

        if (context.UserId.HasValue)
        {
            var user = await _userManager.FindByIdAsync(context.UserId.Value.ToString());
            if (user is not null && !string.IsNullOrEmpty(user.BoundDeviceFingerprint))
            {
                if (user.BoundDeviceFingerprint != context.DeviceFingerprintHash)
                {
                    reasons.Add("Login from unrecognized device.");
                    riskScore += 60;
                }
            }

            // Check for multiple recent failed attempts from different IPs
            var recentAttempts = await _db.SuspiciousLoginAttempts
                .Where(a => a.UserId == context.UserId && a.AttemptedAt >= DateTimeOffset.UtcNow.AddHours(-1))
                .CountAsync(ct);

            if (recentAttempts >= 3)
            {
                reasons.Add("Multiple suspicious login attempts in the last hour.");
                riskScore += 40;
            }
        }

        return new SuspicionAssessment(
            IsSuspicious: riskScore > 0,
            ShouldBlock: riskScore >= 100,
            Reasons: reasons,
            RiskScore: riskScore);
    }
}
