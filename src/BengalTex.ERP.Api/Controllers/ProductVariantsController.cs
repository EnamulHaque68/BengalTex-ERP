using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.ProductVariants;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Product variants (catalog SKU breakdown — color/size/SKU/price per product).
/// Reuses the Products permission group. v1 is catalog-only (not stock-keeping).
/// </summary>
[ApiController]
[Route("api/product-variants")]
[Authorize]
public class ProductVariantsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductVariantsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Products.View)]
    public async Task<IActionResult> GetByProduct([FromQuery] int productId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetProductVariantsQuery(productId), ct));

    [HttpPost]
    [HasPermission(Permissions.Products.Create)]
    public async Task<IActionResult> Create([FromBody] CreateProductVariantCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPost("bulk")]
    [HasPermission(Permissions.Products.Create)]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateProductVariantsCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Products.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductVariantCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Products.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteProductVariantCommand(id), ct));
}
