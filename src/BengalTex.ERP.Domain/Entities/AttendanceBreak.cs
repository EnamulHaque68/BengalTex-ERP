using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A break period within a day's attendance (Break Out → Break In). Multiple breaks per day
/// are supported; the sum of break minutes is deducted from worked time.
/// </summary>
public class AttendanceBreak : BaseTransactionalEntity
{
    public long AttendanceRecordId { get; set; }
    public AttendanceRecord AttendanceRecord { get; set; } = null!;

    /// <summary>"HH:mm" — when the break started (Break Out).</summary>
    public string? BreakOutTime { get; set; }
    /// <summary>"HH:mm" — when the break ended (Break In). Null while on break.</summary>
    public string? BreakInTime { get; set; }

    /// <summary>Break length in minutes (set when Break In is recorded).</summary>
    public int? Minutes { get; set; }

    public int SortOrder { get; set; }
}
