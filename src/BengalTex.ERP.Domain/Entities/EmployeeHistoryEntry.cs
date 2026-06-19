using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A dated event in an employee's service record — salary increment, promotion, transfer, or a
/// disciplinary/warning action. One flexible log entity (type discriminator + from/to/amount)
/// instead of three near-identical tables. Append-only record (the actual salary/designation
/// change is applied separately on the Employee master); used for HR history reporting.
/// </summary>
public class EmployeeHistoryEntry : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public EmployeeHistoryType Type { get; set; }

    public DateOnly EffectiveDate { get; set; }

    /// <summary>Short summary, e.g. "Annual increment 2026" / "Promoted to Senior Operator".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Before value — old salary (increment) or old designation/department (promotion/transfer).</summary>
    public string? FromValue { get; set; }

    /// <summary>After value — new salary or new designation/department.</summary>
    public string? ToValue { get; set; }

    /// <summary>Optional numeric — increment amount or new gross salary.</summary>
    public decimal? Amount { get; set; }

    public string? Details { get; set; }
}

public enum EmployeeHistoryType
{
    Increment = 1,
    Promotion = 2,
    Transfer = 3,
    Disciplinary = 4
}
