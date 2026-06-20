using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.RawMaterialSubstitutes;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Raw-material substitutes (alternative materials) — approved swaps used when a primary material
/// is short. Material-level catalog. Reuses the RawMaterials permission group.
/// </summary>
[ApiController]
[Route("api/raw-material-substitutes")]
[Authorize]
public class RawMaterialSubstitutesController : ControllerBase
{
    private readonly IMediator _mediator;
    public RawMaterialSubstitutesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.RawMaterials.View)]
    public async Task<IActionResult> GetForMaterial([FromQuery] int rawMaterialId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetRawMaterialSubstitutesQuery(rawMaterialId), ct));

    [HttpPost]
    [HasPermission(Permissions.RawMaterials.Create)]
    public async Task<IActionResult> Create([FromBody] CreateRawMaterialSubstituteCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.RawMaterials.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRawMaterialSubstituteCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.RawMaterials.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteRawMaterialSubstituteCommand(id), ct));
}
