using BengalTex.ERP.Application.Company.Commands;
using BengalTex.ERP.Application.Company.Queries;
using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/company")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompanyController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET /api/company — get the singleton company profile</summary>
    [HttpGet]
    [HasPermission(Permissions.Companies.View)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCompanyQuery(), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/company — upsert company profile (create on first run)</summary>
    [HttpPut]
    [HasPermission(Permissions.Companies.Edit)]
    public async Task<IActionResult> Update([FromBody] UpdateCompanyRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCompanyCommand(
            request.Name, request.ShortName, request.RegistrationNumber, request.TaxNumber,
            request.AddressLine1, request.AddressLine2, request.City, request.District,
            request.PostalCode, request.Country, request.Phone, request.Email,
            request.Website, request.LogoUrl
        ), ct);
        return Ok(result);
    }
}

public record UpdateCompanyRequest(
    string Name,
    string ShortName,
    string? RegistrationNumber,
    string? TaxNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string District,
    string? PostalCode,
    string Country,
    string? Phone,
    string? Email,
    string? Website,
    string? LogoUrl
);
