using BengalTex.ERP.Application.Common.Interfaces;
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
    private readonly IFileStorage _files;

    public CompanyController(IMediator mediator, IFileStorage files)
    { _mediator = mediator; _files = files; }

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

    // ── Company logo: upload once → auto-used on invoices, payslips, reports, app branding ──

    /// <summary>Uploads/replaces the company logo (image). Stored via IFileStorage, path saved on Company.LogoUrl.</summary>
    [HttpPost("logo")]
    [HasPermission(Permissions.Companies.Edit)]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file provided.");
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only image files are allowed.");
        await using var stream = file.OpenReadStream();
        var stored = await _files.SaveAsync(stream, file.FileName, file.ContentType, "Company", ct);
        await _mediator.Send(new SetCompanyLogoCommand(stored.StoragePath), ct);
        return Ok(await _mediator.Send(new GetCompanyQuery(), ct));
    }

    /// <summary>Serves the company logo image. Public (logo is branding) so it can be shown on the login page too.</summary>
    [HttpGet("logo")]
    [AllowAnonymous]
    public async Task<IActionResult> Logo(CancellationToken ct)
    {
        var path = await _mediator.Send(new GetCompanyLogoPathQuery(), ct);
        if (string.IsNullOrEmpty(path) || !await _files.ExistsAsync(path, ct)) return NotFound();
        var stream = await _files.OpenReadAsync(path, ct);
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var contentType = ext switch { ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", ".svg" => "image/svg+xml", _ => "image/jpeg" };
        return File(stream, contentType);
    }

    /// <summary>Removes the company logo.</summary>
    [HttpDelete("logo")]
    [HasPermission(Permissions.Companies.Edit)]
    public async Task<IActionResult> DeleteLogo(CancellationToken ct)
    {
        var path = await _mediator.Send(new GetCompanyLogoPathQuery(), ct);
        await _mediator.Send(new SetCompanyLogoCommand(null), ct);
        if (!string.IsNullOrEmpty(path)) { try { await _files.DeleteAsync(path, ct); } catch { /* best-effort */ } }
        return Ok(await _mediator.Send(new GetCompanyQuery(), ct));
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
