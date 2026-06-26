using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Mrp.Commands;
using BengalTex.ERP.Application.Mrp.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Material Requirement Planning (Phase 3). Read-only net-requirements run + one-click draft
/// Purchase Requisition for the shortages. Reuses existing Production + PurchaseRequisition perms.
/// </summary>
[ApiController]
[Route("api/mrp")]
[Authorize]
public class MrpController : ControllerBase
{
    private readonly IMediator _mediator;

    public MrpController(IMediator mediator) => _mediator = mediator;

    /// <summary>The MRP run — per-raw-material Required / OnHand / Available / Incoming / Shortage.</summary>
    [HttpGet]
    [HasPermission(Permissions.Production.View)]
    public async Task<IActionResult> Get([FromQuery] bool shortageOnly = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetMrpQuery(shortageOnly), ct));

    /// <summary>Raise a draft Purchase Requisition covering the selected materials' shortages.</summary>
    [HttpPost("generate-requisition")]
    [HasPermission(Permissions.PurchaseRequisitions.Create)]
    public async Task<IActionResult> GenerateRequisition([FromBody] GenerateMrpRequisitionBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new GeneratePurchaseRequisitionFromMrpCommand(body.RawMaterialIds), ct));
}

public record GenerateMrpRequisitionBody(IReadOnlyList<int> RawMaterialIds);
