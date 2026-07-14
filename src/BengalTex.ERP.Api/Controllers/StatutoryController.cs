using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Accounting.Statutory;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Phase A5b — statutory withholding: outstanding AIT / VDS / PF payable balances and the
/// remittance (challan) register that clears them (Dr payable / Cr Cash|Bank).
/// </summary>
[ApiController]
[Route("api/statutory")]
[Authorize]
public class StatutoryController : ControllerBase
{
    private readonly IMediator _mediator;
    public StatutoryController(IMediator mediator) => _mediator = mediator;

    /// <summary>Outstanding AIT / VDS / PF payable balances as of a date (defaults to today).</summary>
    [HttpGet("liabilities")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> Liabilities([FromQuery] DateOnly? asOfDate = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetStatutoryLiabilitiesQuery(asOfDate), ct));

    /// <summary>Remittance (challan) register, optionally filtered by tax type.</summary>
    [HttpGet("remittances")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> Remittances([FromQuery] string? taxType = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetStatutoryRemittancesQuery(taxType), ct));

    /// <summary>Remit a withheld liability on a challan — posts Dr 2160|2170|2135 / Cr Cash|Bank.</summary>
    [HttpPost("remittances")]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> Remit([FromBody] PostStatutoryRemittanceCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));
}
