using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Compliance.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Compliance.Commands;

// ── List ──
public sealed record GetCertificatesQuery(
    PagedQueryParameters Parameters,
    string? CertificateType = null,
    string? ExpiryStatus = null,         // "Active" | "ExpiringSoon" | "Expired"
    bool IncludeInactive = false
) : IRequest<ApiResponse<PagedResult<ComplianceCertificateDto>>>;

internal sealed class GetCertificatesQueryHandler
    : IRequestHandler<GetCertificatesQuery, ApiResponse<PagedResult<ComplianceCertificateDto>>>
{
    private readonly IRepository<ComplianceCertificate> _repo;
    private readonly IDateTimeProvider _clock;
    public GetCertificatesQueryHandler(IRepository<ComplianceCertificate> repo, IDateTimeProvider clock)
    { _repo = repo; _clock = clock; }

    public async Task<ApiResponse<PagedResult<ComplianceCertificateDto>>> Handle(
        GetCertificatesQuery request, CancellationToken ct)
    {
        var today = _clock.Today;
        var soonCutoff = today.AddDays(ExpiryStatus.ExpiringSoonDays);

        var q = _repo.Query();
        if (!request.IncludeInactive) q = q.Where(c => c.IsActive);
        if (!string.IsNullOrEmpty(request.CertificateType)
            && Enum.TryParse<ComplianceCertificateType>(request.CertificateType, out var t))
            q = q.Where(c => c.CertificateType == t);
        if (!string.IsNullOrEmpty(request.ExpiryStatus))
        {
            q = request.ExpiryStatus switch
            {
                ExpiryStatus.Expired => q.Where(c => c.ExpiryDate < today),
                ExpiryStatus.ExpiringSoonStatus => q.Where(c => c.ExpiryDate >= today && c.ExpiryDate <= soonCutoff),
                ExpiryStatus.Active => q.Where(c => c.ExpiryDate > soonCutoff),
                _ => q
            };
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(c => c.Name.Contains(search) ||
                             (c.CertificateNumber != null && c.CertificateNumber.Contains(search)) ||
                             (c.IssuingAuthority != null && c.IssuingAuthority.Contains(search)));

        q = q.OrderBy(c => c.ExpiryDate);

        var totalCount = await q.CountAsync(ct);
        var rows = await q
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(c => new
            {
                c.Id, c.Name, c.CertificateType, c.IssuingAuthority, c.CertificateNumber,
                c.IssuedDate, c.ExpiryDate, c.Notes, c.IsActive
            })
            .ToListAsync(ct);

        var items = rows.Select(c =>
        {
            var days = c.ExpiryDate.DayNumber - today.DayNumber;
            return new ComplianceCertificateDto(
                c.Id, c.Name, c.CertificateType.ToString(),
                c.IssuingAuthority, c.CertificateNumber,
                c.IssuedDate, c.ExpiryDate, days,
                ExpiryStatus.ClassifyDays(days),
                c.Notes, c.IsActive);
        }).ToList();

        var result = PagedResult<ComplianceCertificateDto>.Create(items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<ComplianceCertificateDto>>.Ok(result);
    }
}

// ── Create ──
public sealed record CreateCertificateCommand(
    string Name, string CertificateType,
    string? IssuingAuthority, string? CertificateNumber,
    DateOnly IssuedDate, DateOnly ExpiryDate, string? Notes
) : IRequest<ApiResponse<int>>;

public sealed class CreateCertificateCommandValidator : AbstractValidator<CreateCertificateCommand>
{
    public CreateCertificateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CertificateType).NotEmpty()
            .Must(s => Enum.TryParse<ComplianceCertificateType>(s, out _))
            .WithMessage("Invalid CertificateType.");
        RuleFor(x => x.IssuingAuthority).MaximumLength(200);
        RuleFor(x => x.CertificateNumber).MaximumLength(100);
        RuleFor(x => x.IssuedDate).NotEmpty();
        RuleFor(x => x.ExpiryDate).GreaterThanOrEqualTo(x => x.IssuedDate);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateCertificateCommandHandler : IRequestHandler<CreateCertificateCommand, ApiResponse<int>>
{
    private readonly IRepository<ComplianceCertificate> _repo;
    private readonly IUnitOfWork _uow;
    public CreateCertificateCommandHandler(IRepository<ComplianceCertificate> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateCertificateCommand cmd, CancellationToken ct)
    {
        var e = new ComplianceCertificate
        {
            Name = cmd.Name.Trim(),
            CertificateType = Enum.Parse<ComplianceCertificateType>(cmd.CertificateType),
            IssuingAuthority = string.IsNullOrWhiteSpace(cmd.IssuingAuthority) ? null : cmd.IssuingAuthority.Trim(),
            CertificateNumber = string.IsNullOrWhiteSpace(cmd.CertificateNumber) ? null : cmd.CertificateNumber.Trim(),
            IssuedDate = cmd.IssuedDate,
            ExpiryDate = cmd.ExpiryDate,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            IsActive = true
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Certificate created.");
    }
}

// ── Update ──
public sealed record UpdateCertificateCommand(
    int Id, string Name, string CertificateType,
    string? IssuingAuthority, string? CertificateNumber,
    DateOnly IssuedDate, DateOnly ExpiryDate, string? Notes, bool IsActive
) : IRequest<ApiResponse<int>>;

public sealed class UpdateCertificateCommandValidator : AbstractValidator<UpdateCertificateCommand>
{
    public UpdateCertificateCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CertificateType).NotEmpty()
            .Must(s => Enum.TryParse<ComplianceCertificateType>(s, out _))
            .WithMessage("Invalid CertificateType.");
        RuleFor(x => x.IssuingAuthority).MaximumLength(200);
        RuleFor(x => x.CertificateNumber).MaximumLength(100);
        RuleFor(x => x.IssuedDate).NotEmpty();
        RuleFor(x => x.ExpiryDate).GreaterThanOrEqualTo(x => x.IssuedDate);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateCertificateCommandHandler : IRequestHandler<UpdateCertificateCommand, ApiResponse<int>>
{
    private readonly IRepository<ComplianceCertificate> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateCertificateCommandHandler(IRepository<ComplianceCertificate> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateCertificateCommand cmd, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(cmd.Id, ct);
        if (c is null) return ApiResponse<int>.Fail("Certificate not found.");
        c.Name = cmd.Name.Trim();
        c.CertificateType = Enum.Parse<ComplianceCertificateType>(cmd.CertificateType);
        c.IssuingAuthority = string.IsNullOrWhiteSpace(cmd.IssuingAuthority) ? null : cmd.IssuingAuthority.Trim();
        c.CertificateNumber = string.IsNullOrWhiteSpace(cmd.CertificateNumber) ? null : cmd.CertificateNumber.Trim();
        c.IssuedDate = cmd.IssuedDate;
        c.ExpiryDate = cmd.ExpiryDate;
        c.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        c.IsActive = cmd.IsActive;
        _repo.Update(c);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(c.Id, "Certificate updated.");
    }
}

// ── Delete ──
public sealed record DeleteCertificateCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteCertificateCommandHandler : IRequestHandler<DeleteCertificateCommand, ApiResponse>
{
    private readonly IRepository<ComplianceCertificate> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteCertificateCommandHandler(IRepository<ComplianceCertificate> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteCertificateCommand cmd, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(cmd.Id, ct);
        if (c is null) return ApiResponse.Fail("Certificate not found.");
        _repo.Remove(c);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Certificate deleted.");
    }
}
