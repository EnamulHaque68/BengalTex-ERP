using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Supplier.Commands;
using BengalTex.ERP.Application.Supplier.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SuppliersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Suppliers.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSuppliersQuery(parameters, includeInactive), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Suppliers.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSupplierByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Suppliers.Create)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSupplierCommand(
            request.Code, request.Name, request.ContactPerson, request.Phone, request.Email, request.Website,
            request.AddressLine1, request.AddressLine2, request.City, request.District, request.PostalCode, request.Country,
            request.BinNumber, request.VatNumber, request.TinNumber,
            request.PaymentTermsDays,
            request.BankName, request.BankAccountNumber, request.BankBranch, request.BankAccountHolderName,
            request.Rating, request.Notes
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Suppliers.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateSupplierCommand(
            id, request.Name, request.ContactPerson, request.Phone, request.Email, request.Website,
            request.AddressLine1, request.AddressLine2, request.City, request.District, request.PostalCode, request.Country,
            request.BinNumber, request.VatNumber, request.TinNumber,
            request.PaymentTermsDays,
            request.BankName, request.BankAccountNumber, request.BankBranch, request.BankAccountHolderName,
            request.Rating, request.Notes, request.IsActive
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Suppliers.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteSupplierCommand(id), ct);
        return Ok(result);
    }
}

public record CreateSupplierRequest(
    string? Code, string Name,
    string? ContactPerson, string? Phone, string? Email, string? Website,
    string AddressLine1, string? AddressLine2, string City, string? District, string? PostalCode, string Country,
    string? BinNumber, string? VatNumber, string? TinNumber,
    int PaymentTermsDays,
    string? BankName, string? BankAccountNumber, string? BankBranch, string? BankAccountHolderName,
    int Rating, string? Notes);

public record UpdateSupplierRequest(
    string Name,
    string? ContactPerson, string? Phone, string? Email, string? Website,
    string AddressLine1, string? AddressLine2, string City, string? District, string? PostalCode, string Country,
    string? BinNumber, string? VatNumber, string? TinNumber,
    int PaymentTermsDays,
    string? BankName, string? BankAccountNumber, string? BankBranch, string? BankAccountHolderName,
    int Rating, string? Notes, bool IsActive);
