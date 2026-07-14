using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Accounting.InventoryGL;
using BengalTex.ERP.Application.Accounting.Revaluation;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Phase A2 — Inventory ↔ GL integrity: the one-time GR/IR initialization (catch-up of pre-A2
/// received-not-billed inventory) and the stock-vs-GL tie-out report.
/// </summary>
[ApiController]
[Route("api/inventory-gl")]
[Authorize]
public class InventoryGlController : ControllerBase
{
    private readonly IMediator _mediator;
    public InventoryGlController(IMediator mediator) => _mediator = mediator;

    /// <summary>Preview the received-not-billed value the GR/IR initialization would post.</summary>
    [HttpGet("gr-ir/init-preview")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> InitPreview(CancellationToken ct)
        => Ok(await _mediator.Send(new GetGrIrInitPreviewQuery(), ct));

    /// <summary>Run the one-time GR/IR initialization catch-up voucher.</summary>
    [HttpPost("gr-ir/initialize")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> Initialize([FromBody] InitializeGrIrRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new InitializeGrIrCommand(request.AsOfDate), ct));

    /// <summary>Stock valuation vs GL inventory accounts + open GR/IR schedule, as of a date.</summary>
    [HttpGet("tie-out")]
    [HasPermission(Permissions.Reports.ViewFinance)]
    public async Task<IActionResult> TieOut([FromQuery] DateOnly? asOfDate = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetInventoryGlTieOutQuery(asOfDate), ct));

    // ── Phase A4 — month-close absorption steps ──

    /// <summary>Preview applied conversion vs actual pools for a period (the absorption variance).</summary>
    [HttpGet("absorption-preview")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> AbsorptionPreview([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetAbsorptionTrueUpPreviewQuery(fromDate, toDate), ct));

    /// <summary>Post the absorption true-up (relieve applied contra to 5165).</summary>
    [HttpPost("absorption-true-up")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> AbsorptionTrueUp([FromBody] AbsorptionTrueUpRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new PostAbsorptionTrueUpCommand(request.FromDate, request.ToDate, request.PostDate), ct));

    /// <summary>Post the month-end WIP snapshot (auto-reversing next day).</summary>
    [HttpPost("wip-snapshot")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> WipSnapshot([FromBody] WipSnapshotRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new PostWipSnapshotCommand(request.AsOfDate), ct));

    // ── Phase A7b — month-end FC revaluation ──

    /// <summary>Preview unrealized FX on open foreign-currency AR/AP at the as-of dated rate.</summary>
    [HttpGet("fx-revaluation-preview")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> FxRevaluationPreview([FromQuery] DateOnly asOfDate, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetFxRevaluationPreviewQuery(asOfDate), ct));

    /// <summary>Post the month-end FC revaluation to 4310/5810 (auto-reversing next day).</summary>
    [HttpPost("fx-revaluation")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> FxRevaluation([FromBody] WipSnapshotRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new PostFxRevaluationCommand(request.AsOfDate), ct));
}

public record InitializeGrIrRequest(DateOnly AsOfDate);
public record AbsorptionTrueUpRequest(DateOnly FromDate, DateOnly ToDate, DateOnly PostDate);
public record WipSnapshotRequest(DateOnly AsOfDate);
