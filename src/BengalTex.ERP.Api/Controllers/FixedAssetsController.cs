using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.FixedAssets.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/fixed-assets")]
[Authorize]
public class FixedAssetsController : ControllerBase
{
    private readonly IMediator _mediator;
    public FixedAssetsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.FixedAssets.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetFixedAssetsQuery(parameters, status, category), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.FixedAssets.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetFixedAssetByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.FixedAssets.Create)]
    public async Task<IActionResult> Create([FromBody] CreateFixedAssetCommand cmd, CancellationToken ct)
        => Ok(await _mediator.Send(cmd, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.FixedAssets.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateFixedAssetBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateFixedAssetCommand(
            id, body.Name, body.Category, body.Location, body.MachineId,
            body.AcquisitionDate, body.AcquisitionCost, body.SalvageValue, body.UsefulLifeYears, body.Notes), ct));

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.FixedAssets.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteFixedAssetCommand(id), ct));

    /// <summary>Run monthly depreciation for all eligible assets — posts journal automatically.</summary>
    [HttpPost("run-depreciation")]
    [HasPermission(Permissions.FixedAssets.Depreciate)]
    public async Task<IActionResult> RunDepreciation([FromBody] RunDepreciationBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new RunDepreciationCommand(body.Year, body.Month), ct));

    [HttpPost("{id:long}/dispose")]
    [HasPermission(Permissions.FixedAssets.Dispose)]
    public async Task<IActionResult> Dispose(long id, [FromBody] DisposeBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new DisposeFixedAssetCommand(
            id, body.DisposalDate, body.DisposalProceeds, body.Notes, body.IsWriteOff), ct));

    [HttpGet("depreciation-runs")]
    [HasPermission(Permissions.FixedAssets.View)]
    public async Task<IActionResult> GetRuns([FromQuery] PagedQueryParameters parameters, CancellationToken ct)
        => Ok(await _mediator.Send(new GetDepreciationRunsQuery(parameters), ct));

    [HttpGet("depreciation-runs/{id:long}")]
    [HasPermission(Permissions.FixedAssets.View)]
    public async Task<IActionResult> GetRunById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetDepreciationRunByIdQuery(id), ct));
}

public record UpdateFixedAssetBody(
    string Name, string Category, string? Location, int? MachineId,
    DateOnly AcquisitionDate, decimal AcquisitionCost, decimal SalvageValue,
    int UsefulLifeYears, string? Notes);

public record RunDepreciationBody(int Year, int Month);

public record DisposeBody(DateOnly DisposalDate, decimal DisposalProceeds, string? Notes, bool IsWriteOff);
