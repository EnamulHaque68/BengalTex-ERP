using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Gate Pass — security/compliance log of EVERY in/out movement through the factory gate.
/// Captures vehicles, drivers, visitors, and materials. Distinct from Delivery Notes /
/// Stock Movements (which handle stock impact); a Gate Pass is the SECURITY ARTEFACT issued
/// at the gate. Audit trail for compliance audits (BSCI/Sedex/WRAP) and for traceability
/// when something goes missing or doesn't come back.
///
/// Type drives workflow:
///   • <see cref="GatePassType.ReturnableOut"/> — material leaving temporarily (subcontract,
///     machine repair, sample to buyer). Must be marked Returned when it comes back.
///   • <see cref="GatePassType.NonReturnableOut"/> — material leaving permanently (delivery,
///     scrap disposal, supplier return). Closed on issue; no return expected.
///   • <see cref="GatePassType.InwardReceipt"/> — material coming in (e.g. GRN-driven receipt,
///     samples in). Records arrival for audit.
///   • <see cref="GatePassType.Visitor"/> — person entering/leaving (visitor log).
///   • <see cref="GatePassType.Vehicle"/> — vehicle in/out without specific cargo logging.
///
/// Source linkage (polymorphic, optional): a Gate Pass can be tagged with the source
/// document that triggered it (DeliveryNote, SupplierReturnNote, SubcontractOrder, GRN)
/// via <see cref="SourceType"/> + <see cref="SourceId"/> + <see cref="SourceCode"/> —
/// free-text reference, no FK (to keep this entity decoupled).
/// </summary>
public class GatePass : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;   // "GP-####"

    public DateOnly PassDate { get; set; }
    public TimeOnly? PassTime { get; set; }            // when issued at the gate

    public GatePassType Type { get; set; } = GatePassType.NonReturnableOut;

    /// <summary>Effective In/Out direction (most types are Out; Inward Receipt + some Visitor are In).</summary>
    public GatePassDirection Direction { get; set; } = GatePassDirection.Out;

    // ── Vehicle / driver block ─────────────────────────────────────────────
    public string? VehicleNumber { get; set; }         // "DHK-MA-1234"
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string? DriverNidNumber { get; set; }       // BD National ID (optional log)
    public string? TransporterName { get; set; }       // logistics company / owner

    // ── Visitor block (for visitor passes) ─────────────────────────────────
    public string? VisitorName { get; set; }
    public string? VisitorPhone { get; set; }
    public string? VisitorOrganization { get; set; }
    public string? VisitorPurpose { get; set; }

    // ── Material block (for material passes) ───────────────────────────────
    public string? ItemDescription { get; set; }       // free-text: "10 cartons of woven labels"
    public string? Quantity { get; set; }              // free-text qty + uom (avoid coupling to RM/Product master)

    public string? FromLocation { get; set; }          // e.g. warehouse name / factory zone
    public string? ToLocation { get; set; }            // destination / origin

    // ── Polymorphic source reference (optional) ────────────────────────────
    public string? SourceType { get; set; }            // "DeliveryNote" / "SupplierReturnNote" / "SubcontractOrder" / "GoodsReceiptNote"
    public long? SourceId { get; set; }
    public string? SourceCode { get; set; }            // human-readable code for display

    // ── Authorization ──────────────────────────────────────────────────────
    public string? IssuedByUser { get; set; }          // gate guard / security
    public string? ApprovedByUser { get; set; }        // shift in-charge

    // ── Returnable workflow ────────────────────────────────────────────────
    public DateOnly? ExpectedReturnDate { get; set; }   // only meaningful when Type = ReturnableOut
    public DateTimeOffset? ReturnedAt { get; set; }
    public string? ReturnedByUser { get; set; }
    public string? ReturnNotes { get; set; }            // damaged / missing / partial details

    public DateTimeOffset? ClosedAt { get; set; }       // for Non-returnable / Visitor / Vehicle types

    public GatePassStatus Status { get; set; } = GatePassStatus.Open;

    public string? Notes { get; set; }
}

public enum GatePassType
{
    NonReturnableOut = 1,
    ReturnableOut = 2,
    InwardReceipt = 3,
    Visitor = 4,
    Vehicle = 5
}

public enum GatePassDirection
{
    Out = 1,
    In = 2
}

public enum GatePassStatus
{
    Open = 1,        // initial state
    Returned = 2,    // ReturnableOut closed via mark-returned
    Closed = 3,      // NonReturnableOut/Visitor/Vehicle/InwardReceipt closed cleanly
    Overdue = 4,     // ReturnableOut where today > ExpectedReturnDate (computed, not stored — but useful at row level)
    Cancelled = 5
}
