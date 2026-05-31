using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A scheduled or completed compliance audit — buyer audit (Walmart, H&amp;M, etc.),
/// 3rd-party (BSCI/Sedex/WRAP), or internal. Findings are captured as
/// <see cref="AuditFinding"/> rows that drive the CAP (Corrective Action Plan).
/// </summary>
public class ComplianceAudit : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;     // "AUD-####"

    public ComplianceAuditType AuditType { get; set; }

    /// <summary>Auditing org or person, free text (e.g. "SGS BSCI Audit", "H&amp;M sustainability team").</summary>
    public string Auditor { get; set; } = string.Empty;

    public DateOnly ScheduledDate { get; set; }
    public DateOnly? ActualDate { get; set; }

    public ComplianceAuditStatus Status { get; set; } = ComplianceAuditStatus.Scheduled;
    public ComplianceAuditResult? Result { get; set; }
    public decimal? Score { get; set; }   // 0-100, optional

    public string? Notes { get; set; }

    public ICollection<AuditFinding> Findings { get; set; } = new List<AuditFinding>();
}

/// <summary>
/// One CAP (Corrective Action Plan) item against a <see cref="ComplianceAudit"/>.
/// Tracked individually until closed — overdue items are flagged in the dashboard.
/// </summary>
public class AuditFinding : BaseTransactionalEntity
{
    public long ComplianceAuditId { get; set; }
    public ComplianceAudit ComplianceAudit { get; set; } = null!;

    public string FindingDescription { get; set; } = string.Empty;
    public AuditFindingSeverity Severity { get; set; } = AuditFindingSeverity.Minor;

    public string? CorrectiveAction { get; set; }

    /// <summary>Responsible person (Employee FK; nullable to allow team-level finding).</summary>
    public int? AssignedToEmployeeId { get; set; }
    public Employee? AssignedToEmployee { get; set; }

    public DateOnly? DueDate { get; set; }
    public DateOnly? ClosureDate { get; set; }

    public AuditFindingStatus Status { get; set; } = AuditFindingStatus.Open;

    public string? Notes { get; set; }
}

public enum ComplianceAuditType
{
    BSCI = 1,
    Sedex = 2,
    WRAP = 3,
    SA8000 = 4,
    BuyerAudit = 5,
    Internal = 6,
    Government = 7,
    Other = 99
}

public enum ComplianceAuditStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum ComplianceAuditResult
{
    Pass = 1,
    Conditional = 2,        // pass with corrective action plan
    Fail = 3,
    PendingCorrection = 4   // result pending until CAP is closed
}

public enum AuditFindingSeverity
{
    Critical = 1,
    Major = 2,
    Minor = 3,
    Observation = 4
}

public enum AuditFindingStatus
{
    Open = 1,
    InProgress = 2,
    Closed = 3,
    Waived = 4
}
