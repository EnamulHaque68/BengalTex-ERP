using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Payroll.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payroll.Commands;

// ── List ──
public sealed record GetEmployeeLoansQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    int? EmployeeId = null
) : IRequest<ApiResponse<PagedResult<EmployeeLoanDto>>>;

internal sealed class GetEmployeeLoansQueryHandler
    : IRequestHandler<GetEmployeeLoansQuery, ApiResponse<PagedResult<EmployeeLoanDto>>>
{
    private readonly IRepository<EmployeeLoan, long> _repo;
    public GetEmployeeLoansQueryHandler(IRepository<EmployeeLoan, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<EmployeeLoanDto>>> Handle(
        GetEmployeeLoansQuery request, CancellationToken ct)
    {
        var query = _repo.Query();
        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<EmployeeLoanStatus>(request.Status, out var s))
            query = query.Where(l => l.Status == s);
        if (request.EmployeeId.HasValue) query = query.Where(l => l.EmployeeId == request.EmployeeId.Value);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(l => l.Code.Contains(search) ||
                                     l.Employee.Code.Contains(search) ||
                                     l.Employee.FullName.Contains(search));

        query = query.OrderByDescending(l => l.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(l => new EmployeeLoanDto(
                l.Id, l.Code, l.EmployeeId, l.Employee.Code, l.Employee.FullName,
                l.IssuedDate, l.Principal, l.EmiAmount, l.TenureMonths, l.StartYearMonth,
                l.OutstandingPrincipal, l.Principal - l.OutstandingPrincipal,
                l.Status.ToString(), l.Notes))
            .ToListAsync(ct);

        var result = PagedResult<EmployeeLoanDto>.Create(items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<EmployeeLoanDto>>.Ok(result);
    }
}

// ── Create ──
public sealed record CreateEmployeeLoanCommand(
    int EmployeeId,
    DateOnly IssuedDate,
    decimal Principal,
    decimal EmiAmount,
    int TenureMonths,
    int StartYearMonth,        // YYYYMM e.g. 202607
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class CreateEmployeeLoanCommandValidator : AbstractValidator<CreateEmployeeLoanCommand>
{
    public CreateEmployeeLoanCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Principal).GreaterThan(0);
        RuleFor(x => x.EmiAmount).GreaterThan(0);
        RuleFor(x => x.TenureMonths).GreaterThan(0).LessThanOrEqualTo(120);
        RuleFor(x => x.StartYearMonth).InclusiveBetween(200001, 210012)
            .Must(ym => ym % 100 >= 1 && ym % 100 <= 12).WithMessage("StartYearMonth month part must be 01-12.");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateEmployeeLoanCommandHandler : IRequestHandler<CreateEmployeeLoanCommand, ApiResponse<long>>
{
    private readonly IRepository<EmployeeLoan, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateEmployeeLoanCommandHandler(IRepository<EmployeeLoan, long> repo,
        IRepository<Domain.Entities.Employee> empRepo, IUnitOfWork uow, INumberingService numbering)
    { _repo = repo; _empRepo = empRepo; _uow = uow; _numbering = numbering; }

    public async Task<ApiResponse<long>> Handle(CreateEmployeeLoanCommand cmd, CancellationToken ct)
    {
        if (!await _empRepo.Query().AnyAsync(e => e.Id == cmd.EmployeeId && e.IsActive, ct))
            return ApiResponse<long>.Fail("Employee not found or inactive.");

        var code = await _numbering.NextAsync("LN", null, ct);
        var loan = new EmployeeLoan
        {
            Code = code,
            EmployeeId = cmd.EmployeeId,
            IssuedDate = cmd.IssuedDate,
            Principal = cmd.Principal,
            EmiAmount = cmd.EmiAmount,
            TenureMonths = cmd.TenureMonths,
            StartYearMonth = cmd.StartYearMonth,
            OutstandingPrincipal = cmd.Principal,
            Status = EmployeeLoanStatus.Active,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(loan, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(loan.Id, "Loan created.");
    }
}

// ── Update (terms — Active only) ──
public sealed record UpdateEmployeeLoanCommand(long Id, decimal EmiAmount, int TenureMonths, int StartYearMonth, string? Notes)
    : IRequest<ApiResponse>;

public sealed class UpdateEmployeeLoanCommandValidator : AbstractValidator<UpdateEmployeeLoanCommand>
{
    public UpdateEmployeeLoanCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.EmiAmount).GreaterThan(0);
        RuleFor(x => x.TenureMonths).GreaterThan(0).LessThanOrEqualTo(120);
        RuleFor(x => x.StartYearMonth).InclusiveBetween(200001, 210012);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class UpdateEmployeeLoanCommandHandler : IRequestHandler<UpdateEmployeeLoanCommand, ApiResponse>
{
    private readonly IRepository<EmployeeLoan, long> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateEmployeeLoanCommandHandler(IRepository<EmployeeLoan, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(UpdateEmployeeLoanCommand cmd, CancellationToken ct)
    {
        var l = await _repo.GetByIdAsync(cmd.Id, ct);
        if (l is null) return ApiResponse.Fail("Loan not found.");
        if (l.Status != EmployeeLoanStatus.Active)
            return ApiResponse.Fail($"Cannot edit a {l.Status} loan.");
        l.EmiAmount = cmd.EmiAmount;
        l.TenureMonths = cmd.TenureMonths;
        l.StartYearMonth = cmd.StartYearMonth;
        l.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        _repo.Update(l);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Loan updated.");
    }
}

// ── Close (manual close / write-off remaining) ──
public sealed record CloseEmployeeLoanCommand(long Id, bool Cancel = false) : IRequest<ApiResponse>;

internal sealed class CloseEmployeeLoanCommandHandler : IRequestHandler<CloseEmployeeLoanCommand, ApiResponse>
{
    private readonly IRepository<EmployeeLoan, long> _repo;
    private readonly IUnitOfWork _uow;
    public CloseEmployeeLoanCommandHandler(IRepository<EmployeeLoan, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(CloseEmployeeLoanCommand cmd, CancellationToken ct)
    {
        var l = await _repo.GetByIdAsync(cmd.Id, ct);
        if (l is null) return ApiResponse.Fail("Loan not found.");
        if (l.Status != EmployeeLoanStatus.Active)
            return ApiResponse.Fail($"Loan is already {l.Status}.");
        l.Status = cmd.Cancel ? EmployeeLoanStatus.Cancelled : EmployeeLoanStatus.Closed;
        _repo.Update(l);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Loan {l.Status.ToString().ToLowerInvariant()}.");
    }
}
