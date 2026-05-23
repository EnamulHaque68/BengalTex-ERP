using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A buyer sample request and its development/approval lifecycle — the pre-bulk stage of
/// garments-accessories work (a buyer must approve a physical sample before a bulk order).
/// Lifecycle: Requested → InDevelopment → Submitted → Approved | Rejected. Lead time is the
/// days from request to submission. Sample images / spec sheets attach via the polymorphic
/// document-attachment panel (EntityType = "Sample"). An approved sample is the basis for a
/// Quotation / Sales Order (linked manually in v1).
/// </summary>
public class Sample : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Optional existing product this sample is based on.</summary>
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Optional buyer style this sample belongs to.</summary>
    public int? StyleId { get; set; }
    public Style? Style { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Buyer's reference for this sample request.</summary>
    public string? BuyerReference { get; set; }

    public decimal Quantity { get; set; }

    public DateOnly RequestedDate { get; set; }
    public DateOnly? TargetDate { get; set; }

    public SampleStatus Status { get; set; } = SampleStatus.Requested;

    public DateTimeOffset? SubmittedAt { get; set; }
    public DateOnly? SubmittedDate { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }

    /// <summary>Buyer feedback captured at approval/rejection.</summary>
    public string? Feedback { get; set; }

    public string? Notes { get; set; }
}

public enum SampleStatus
{
    Requested = 1,
    InDevelopment = 2,
    Submitted = 3,       // sent to buyer, awaiting decision
    Approved = 4,
    Rejected = 5
}
