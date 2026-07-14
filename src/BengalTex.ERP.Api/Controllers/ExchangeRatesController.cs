using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Accounting.ExchangeRates;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>Phase A6c — dated foreign-exchange rate history (source for month-end FC revaluation).</summary>
[ApiController]
[Route("api/exchange-rates")]
[Authorize]
public class ExchangeRatesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ExchangeRatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> GetAll([FromQuery] int? currencyId = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetExchangeRatesQuery(currencyId), ct));

    /// <summary>Resolve a currency's BDT rate as of a date (dated rate, else the currency's current rate).</summary>
    [HttpGet("as-of")]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> AsOf([FromQuery] int currencyId, [FromQuery] DateOnly date, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetRateAsOfQuery(currencyId, date), ct));

    [HttpPost]
    [HasPermission(Permissions.Accounting.CloseBooks)]
    public async Task<IActionResult> Set([FromBody] SetExchangeRateCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));
}
