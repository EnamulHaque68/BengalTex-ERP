using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Calendar holiday — dates excluded from leave-day computation AND used by attendance
/// for the Holiday status. One row per (Date, Name).
/// </summary>
public class Holiday : BaseEntity
{
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
