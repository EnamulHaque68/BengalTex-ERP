using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.ProductCategory.Commands;
using BengalTex.ERP.Application.ProductCategory.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/product-categories")]
[Authorize]
public class ProductCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductCategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.ProductCategories.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductCategoriesQuery(includeInactive), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.ProductCategories.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductCategoryByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.ProductCategories.Create)]
    public async Task<IActionResult> Create([FromBody] CreateProductCategoryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateProductCategoryCommand(
            request.Code, request.Name, request.Description
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.ProductCategories.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductCategoryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateProductCategoryCommand(
            id, request.Name, request.Description, request.IsActive
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.ProductCategories.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteProductCategoryCommand(id), ct);
        return Ok(result);
    }
}

public record CreateProductCategoryRequest(string Code, string Name, string? Description);
public record UpdateProductCategoryRequest(string Name, string? Description, bool IsActive);
