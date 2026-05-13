using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Currency.Commands;
using BengalTex.ERP.Application.Currency.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/currencies")]
[Authorize]
public class CurrenciesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurrenciesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Currencies.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCurrenciesQuery(includeInactive), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Currencies.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCurrencyByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Currencies.Create)]
    public async Task<IActionResult> Create([FromBody] CreateCurrencyRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateCurrencyCommand(
            request.Code, request.Name, request.Symbol,
            request.ExchangeRateToBase, request.IsBaseCurrency
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Currencies.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCurrencyRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCurrencyCommand(
            id, request.Name, request.Symbol,
            request.ExchangeRateToBase, request.IsBaseCurrency, request.IsActive
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Currencies.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteCurrencyCommand(id), ct);
        return Ok(result);
    }
}

public record CreateCurrencyRequest(
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBaseCurrency);

public record UpdateCurrencyRequest(
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBaseCurrency,
    bool IsActive);
