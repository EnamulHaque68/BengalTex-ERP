using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Customer.Commands;
using BengalTex.ERP.Application.Customer.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET /api/customers — paginated list with search across code/name/phone/email.</summary>
    [HttpGet]
    [HasPermission(Permissions.Customers.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCustomersQuery(parameters, includeInactive), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Customers.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Customer's current credit standing (limit / outstanding AR / available) in BDT.</summary>
    [HttpGet("{id:int}/credit-status")]
    [HasPermission(Permissions.Customers.View)]
    public async Task<IActionResult> GetCreditStatus(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCustomerCreditStatusQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Customers.Create)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateCustomerCommand(
            request.Code, request.Name, request.ContactPerson, request.Phone, request.Email, request.Website,
            request.AddressLine1, request.AddressLine2, request.City, request.District, request.PostalCode, request.Country,
            request.BinNumber, request.VatNumber, request.TinNumber,
            request.Category, request.CreditLimit, request.CreditPeriodDays, request.IsExport, request.Notes,
            request.ParentCustomerId
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Customers.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCustomerCommand(
            id, request.Name, request.ContactPerson, request.Phone, request.Email, request.Website,
            request.AddressLine1, request.AddressLine2, request.City, request.District, request.PostalCode, request.Country,
            request.BinNumber, request.VatNumber, request.TinNumber,
            request.Category, request.CreditLimit, request.CreditPeriodDays, request.IsExport, request.Notes, request.IsActive,
            request.ParentCustomerId
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Customers.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteCustomerCommand(id), ct);
        return Ok(result);
    }
}

public record CreateCustomerRequest(
    string? Code,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Website,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? District,
    string? PostalCode,
    string Country,
    string? BinNumber,
    string? VatNumber,
    string? TinNumber,
    string Category,
    decimal CreditLimit,
    int CreditPeriodDays,
    bool IsExport,
    string? Notes,
    int? ParentCustomerId = null);

public record UpdateCustomerRequest(
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Website,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? District,
    string? PostalCode,
    string Country,
    string? BinNumber,
    string? VatNumber,
    string? TinNumber,
    string Category,
    decimal CreditLimit,
    int CreditPeriodDays,
    bool IsExport,
    string? Notes,
    bool IsActive,
    int? ParentCustomerId = null);
