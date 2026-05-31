using BengalTex.ERP.Application.Leaves.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Leaves.Commands;

// ── List ──
public sealed record GetLeaveTypesQuery(bool IncludeInactive = false) : IRequest<ApiResponse<IReadOnlyList<LeaveTypeDto>>>;

internal sealed class GetLeaveTypesQueryHandler : IRequestHandler<GetLeaveTypesQuery, ApiResponse<IReadOnlyList<LeaveTypeDto>>>
{
    private readonly IRepository<LeaveType> _repo;
    public GetLeaveTypesQueryHandler(IRepository<LeaveType> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<LeaveTypeDto>>> Handle(GetLeaveTypesQuery request, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!request.IncludeInactive) q = q.Where(t => t.IsActive);
        var items = await q.OrderBy(t => t.Code)
            .Select(t => new LeaveTypeDto(t.Id, t.Code, t.Name, t.IsPaid, t.AnnualEntitlement, t.MaxConsecutiveDays, t.Description, t.IsActive))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<LeaveTypeDto>>.Ok(items);
    }
}

// ── Create ──
public sealed record CreateLeaveTypeCommand(string Code, string Name, bool IsPaid,
    decimal AnnualEntitlement, int? MaxConsecutiveDays, string? Description) : IRequest<ApiResponse<int>>;

public sealed class CreateLeaveTypeCommandValidator : AbstractValidator<CreateLeaveTypeCommand>
{
    public CreateLeaveTypeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Z0-9-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AnnualEntitlement).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxConsecutiveDays).GreaterThan(0).When(x => x.MaxConsecutiveDays.HasValue);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class CreateLeaveTypeCommandHandler : IRequestHandler<CreateLeaveTypeCommand, ApiResponse<int>>
{
    private readonly IRepository<LeaveType> _repo;
    private readonly IUnitOfWork _uow;
    public CreateLeaveTypeCommandHandler(IRepository<LeaveType> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateLeaveTypeCommand cmd, CancellationToken ct)
    {
        var code = cmd.Code.Trim().ToUpperInvariant();
        if (await _repo.Query().AnyAsync(t => t.Code == code, ct))
            return ApiResponse<int>.Fail($"Leave type '{code}' already exists.");
        var e = new LeaveType
        {
            Code = code, Name = cmd.Name.Trim(), IsPaid = cmd.IsPaid,
            AnnualEntitlement = cmd.AnnualEntitlement, MaxConsecutiveDays = cmd.MaxConsecutiveDays,
            Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim(),
            IsActive = true
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Leave type created.");
    }
}

// ── Update ──
public sealed record UpdateLeaveTypeCommand(int Id, string Name, bool IsPaid,
    decimal AnnualEntitlement, int? MaxConsecutiveDays, string? Description, bool IsActive) : IRequest<ApiResponse<int>>;

public sealed class UpdateLeaveTypeCommandValidator : AbstractValidator<UpdateLeaveTypeCommand>
{
    public UpdateLeaveTypeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AnnualEntitlement).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxConsecutiveDays).GreaterThan(0).When(x => x.MaxConsecutiveDays.HasValue);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class UpdateLeaveTypeCommandHandler : IRequestHandler<UpdateLeaveTypeCommand, ApiResponse<int>>
{
    private readonly IRepository<LeaveType> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateLeaveTypeCommandHandler(IRepository<LeaveType> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateLeaveTypeCommand cmd, CancellationToken ct)
    {
        var t = await _repo.GetByIdAsync(cmd.Id, ct);
        if (t is null) return ApiResponse<int>.Fail("Leave type not found.");
        t.Name = cmd.Name.Trim(); t.IsPaid = cmd.IsPaid;
        t.AnnualEntitlement = cmd.AnnualEntitlement; t.MaxConsecutiveDays = cmd.MaxConsecutiveDays;
        t.Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();
        t.IsActive = cmd.IsActive;
        _repo.Update(t);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(t.Id, "Leave type updated.");
    }
}

// ── Delete ──
public sealed record DeleteLeaveTypeCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteLeaveTypeCommandHandler : IRequestHandler<DeleteLeaveTypeCommand, ApiResponse>
{
    private readonly IRepository<LeaveType> _repo;
    private readonly IRepository<LeaveApplication, long> _appRepo;
    private readonly IUnitOfWork _uow;
    public DeleteLeaveTypeCommandHandler(IRepository<LeaveType> repo, IRepository<LeaveApplication, long> appRepo, IUnitOfWork uow)
    { _repo = repo; _appRepo = appRepo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteLeaveTypeCommand cmd, CancellationToken ct)
    {
        var t = await _repo.GetByIdAsync(cmd.Id, ct);
        if (t is null) return ApiResponse.Fail("Leave type not found.");
        if (await _appRepo.Query().AnyAsync(a => a.LeaveTypeId == cmd.Id, ct))
            return ApiResponse.Fail("This leave type is used by applications (deactivate it instead).");
        _repo.Remove(t);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Leave type deleted.");
    }
}
