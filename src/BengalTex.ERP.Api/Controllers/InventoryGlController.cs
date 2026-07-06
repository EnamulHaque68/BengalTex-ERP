using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Accounting.InventoryGL;
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
}

public record InitializeGrIrRequest(DateOnly AsOfDate);
