using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.CustomerPricing;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Customer-specific selling prices (price list per customer per product). Reuses the Customers
/// permission group. The lookup endpoint feeds Quotation / Sales Order line price auto-fill.
/// </summary>
[ApiController]
[Route("api/customer-pricing")]
[Authorize]
public class CustomerPricingController : ControllerBase
{
    private readonly IMediator _mediator;
    public CustomerPricingController(IMediator mediator) => _mediator = mediator;

    /// <summary>All prices set for a customer.</summary>
    [HttpGet("customer/{customerId:int}")]
    [HasPermission(Permissions.Customers.View)]
    public async Task<IActionResult> GetForCustomer(
        int customerId, [FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCustomerProductPricesQuery(customerId, includeInactive), ct));

    /// <summary>Resolve a single customer+product price (null = none set → use the default).</summary>
    [HttpGet("lookup")]
    [HasPermission(Permissions.Customers.View)]
    public async Task<IActionResult> Lookup(
        [FromQuery] int customerId, [FromQuery] int productId, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCustomerProductPriceQuery(customerId, productId), ct));

    [HttpPost]
    [HasPermission(Permissions.Customers.Edit)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerProductPriceCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Customers.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerProductPriceCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Customers.Edit)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteCustomerProductPriceCommand(id), ct));
}
