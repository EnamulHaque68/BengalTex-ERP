using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Employee.Commands;

// ── DTO ──
public sealed record EmployeeHistoryEntryDto(
    int Id,
    int EmployeeId,
    string Type,
    DateOnly EffectiveDate,
    string Title,
    string? FromValue,
    string? ToValue,
    decimal? Amount,
    string? Details,
    DateTimeOffset CreatedAt,
    string? CreatedBy);

// ── List by employee ──
public sealed record GetEmployeeHistoryQuery(int EmployeeId)
    : IRequest<ApiResponse<IReadOnlyList<EmployeeHistoryEntryDto>>>;

internal sealed class GetEmployeeHistoryQueryHandler
    : IRequestHandler<GetEmployeeHistoryQuery, ApiResponse<IReadOnlyList<EmployeeHistoryEntryDto>>>
{
    private readonly IRepository<EmployeeHistoryEntry> _repo;
    public GetEmployeeHistoryQueryHandler(IRepository<EmployeeHistoryEntry> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<EmployeeHistoryEntryDto>>> Handle(
        GetEmployeeHistoryQuery req, CancellationToken ct)
    {
        var items = await _repo.Query()
            .Where(h => h.EmployeeId == req.EmployeeId)
            .OrderByDescending(h => h.EffectiveDate).ThenByDescending(h => h.Id)
            .Select(h => new EmployeeHistoryEntryDto(
                h.Id, h.EmployeeId, h.Type.ToString(), h.EffectiveDate, h.Title,
                h.FromValue, h.ToValue, h.Amount, h.Details, h.CreatedAt, h.CreatedBy))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<EmployeeHistoryEntryDto>>.Ok(items);
    }
}

// ── Create ──
public sealed record CreateEmployeeHistoryCommand(
    int EmployeeId, string Type, DateOnly EffectiveDate, string Title,
    string? FromValue, string? ToValue, decimal? Amount, string? Details)
    : IRequest<ApiResponse<int>>;

public sealed class CreateEmployeeHistoryCommandValidator : AbstractValidator<CreateEmployeeHistoryCommand>
{
    public CreateEmployeeHistoryCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Type).Must(t => Enum.TryParse<EmployeeHistoryType>(t, out _))
            .WithMessage("Type must be Increment, Promotion, Transfer, or Disciplinary.");
        RuleFor(x => x.EffectiveDate).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FromValue).MaximumLength(200);
        RuleFor(x => x.ToValue).MaximumLength(200);
        RuleFor(x => x.Details).MaximumLength(2000);
    }
}

internal sealed class CreateEmployeeHistoryCommandHandler
    : IRequestHandler<CreateEmployeeHistoryCommand, ApiResponse<int>>
{
    private readonly IRepository<EmployeeHistoryEntry> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IUnitOfWork _uow;

    public CreateEmployeeHistoryCommandHandler(
        IRepository<EmployeeHistoryEntry> repo, IRepository<Domain.Entities.Employee> employeeRepo, IUnitOfWork uow)
    { _repo = repo; _employeeRepo = employeeRepo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateEmployeeHistoryCommand cmd, CancellationToken ct)
    {
        if (await _employeeRepo.GetByIdAsync(cmd.EmployeeId, ct) is null)
            return ApiResponse<int>.Fail("Employee not found.");

        var e = new EmployeeHistoryEntry
        {
            EmployeeId = cmd.EmployeeId,
            Type = Enum.Parse<EmployeeHistoryType>(cmd.Type),
            EffectiveDate = cmd.EffectiveDate,
            Title = cmd.Title.Trim(),
            FromValue = Clean(cmd.FromValue),
            ToValue = Clean(cmd.ToValue),
            Amount = cmd.Amount,
            Details = Clean(cmd.Details)
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "History entry added.");
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

// ── Delete ──
public sealed record DeleteEmployeeHistoryCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteEmployeeHistoryCommandHandler : IRequestHandler<DeleteEmployeeHistoryCommand, ApiResponse>
{
    private readonly IRepository<EmployeeHistoryEntry> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteEmployeeHistoryCommandHandler(IRepository<EmployeeHistoryEntry> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteEmployeeHistoryCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("History entry not found.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("History entry deleted.");
    }
}
