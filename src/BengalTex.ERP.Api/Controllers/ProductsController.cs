using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Product.Commands;
using BengalTex.ERP.Application.Product.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Products.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? categoryId = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductsQuery(parameters, categoryId, includeInactive), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Products.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Products.Create)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateProductCommand(
            request.Code, request.Name, request.Specification,
            request.ProductCategoryId, request.UnitOfMeasureId,
            request.Size, request.Color, request.Material,
            request.SalesPrice, request.ReorderLevel, request.IsStockItem,
            request.ImageUrl, request.Notes
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Products.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateProductCommand(
            id, request.Name, request.Specification,
            request.ProductCategoryId, request.UnitOfMeasureId,
            request.Size, request.Color, request.Material,
            request.SalesPrice, request.ReorderLevel, request.IsStockItem,
            request.ImageUrl, request.Notes, request.IsActive
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Products.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id), ct);
        return Ok(result);
    }
}

public record CreateProductRequest(
    string? Code,
    string Name,
    string? Specification,
    int ProductCategoryId,
    int UnitOfMeasureId,
    string? Size,
    string? Color,
    string? Material,
    decimal SalesPrice,
    decimal ReorderLevel,
    bool IsStockItem,
    string? ImageUrl,
    string? Notes);

public record UpdateProductRequest(
    string Name,
    string? Specification,
    int ProductCategoryId,
    int UnitOfMeasureId,
    string? Size,
    string? Color,
    string? Material,
    decimal SalesPrice,
    decimal ReorderLevel,
    bool IsStockItem,
    string? ImageUrl,
    string? Notes,
    bool IsActive);
