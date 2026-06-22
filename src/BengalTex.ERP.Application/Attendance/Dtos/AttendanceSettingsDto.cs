namespace BengalTex.ERP.Application.Attendance.Dtos;

/// <summary>Company attendance policy (office hours, grace, geo-fence mode, selfie/approval toggles).</summary>
public sealed record AttendanceSettingsDto(
    int Id,
    string OfficeStartTime,        // "HH:mm"
    string OfficeEndTime,
    int GracePeriodMinutes,
    string OutsideFenceMode,       // Flag | Block
    int DefaultRadiusMeters,
    bool RequireSelfie,
    bool RequireSupervisorApproval,
    bool AllowRemote,
    bool AllowFieldVisit);
