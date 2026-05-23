using BengalTex.ERP.Application.Wastage.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Wastage.Commands;

// ── List query ──
public sealed record GetWastageReasonsQuery(bool IncludeInactive = false) : IRequest<ApiResponse<IReadOnlyList<WastageReasonDto>>>;

internal sealed class GetWastageReasonsQueryHandler : IRequestHandler<GetWastageReasonsQuery, ApiResponse<IReadOnlyList<WastageReasonDto>>>
{
    private readonly IRepository<WastageReason> _repo;
    public GetWastageReasonsQueryHandler(IRepository<WastageReason> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<WastageReasonDto>>> Handle(GetWastageReasonsQuery request, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!request.IncludeInactive) q = q.Where(r => r.IsActive);
        var items = await q.OrderBy(r => r.Name)
            .Select(r => new WastageReasonDto(r.Id, r.Name, r.IsReusable, r.IsActive, r.Description))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<WastageReasonDto>>.Ok(items);
    }
}

// ── Create ──
public sealed record CreateWastageReasonCommand(string Name, bool IsReusable, string? Description) : IRequest<ApiResponse<int>>;

public sealed class CreateWastageReasonCommandValidator : AbstractValidator<CreateWastageReasonCommand>
{
    public CreateWastageReasonCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class CreateWastageReasonCommandHandler : IRequestHandler<CreateWastageReasonCommand, ApiResponse<int>>
{
    private readonly IRepository<WastageReason> _repo;
    private readonly IUnitOfWork _uow;
    public CreateWastageReasonCommandHandler(IRepository<WastageReason> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateWastageReasonCommand cmd, CancellationToken ct)
    {
        var name = cmd.Name.Trim();
        if (await _repo.Query().AnyAsync(r => r.Name == name, ct))
            return ApiResponse<int>.Fail($"A wastage reason '{name}' already exists.");
        var e = new WastageReason { Name = name, IsReusable = cmd.IsReusable, IsActive = true, Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim() };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Wastage reason created.");
    }
}

// ── Update ──
public sealed record UpdateWastageReasonCommand(int Id, string Name, bool IsReusable, bool IsActive, string? Description) : IRequest<ApiResponse<int>>;

public sealed class UpdateWastageReasonCommandValidator : AbstractValidator<UpdateWastageReasonCommand>
{
    public UpdateWastageReasonCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class UpdateWastageReasonCommandHandler : IRequestHandler<UpdateWastageReasonCommand, ApiResponse<int>>
{
    private readonly IRepository<WastageReason> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateWastageReasonCommandHandler(IRepository<WastageReason> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateWastageReasonCommand cmd, CancellationToken ct)
    {
        var r = await _repo.GetByIdAsync(cmd.Id, ct);
        if (r is null) return ApiResponse<int>.Fail("Wastage reason not found.");
        var name = cmd.Name.Trim();
        if (name != r.Name && await _repo.Query().AnyAsync(x => x.Name == name && x.Id != cmd.Id, ct))
            return ApiResponse<int>.Fail($"A wastage reason '{name}' already exists.");
        r.Name = name; r.IsReusable = cmd.IsReusable; r.IsActive = cmd.IsActive;
        r.Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();
        _repo.Update(r);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(r.Id, "Wastage reason updated.");
    }
}

// ── Delete ──
public sealed record DeleteWastageReasonCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteWastageReasonCommandHandler : IRequestHandler<DeleteWastageReasonCommand, ApiResponse>
{
    private readonly IRepository<WastageReason> _repo;
    private readonly IRepository<WastageEntry, long> _entryRepo;
    private readonly IUnitOfWork _uow;
    public DeleteWastageReasonCommandHandler(IRepository<WastageReason> repo, IRepository<WastageEntry, long> entryRepo, IUnitOfWork uow)
    { _repo = repo; _entryRepo = entryRepo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteWastageReasonCommand cmd, CancellationToken ct)
    {
        var r = await _repo.GetByIdAsync(cmd.Id, ct);
        if (r is null) return ApiResponse.Fail("Wastage reason not found.");
        if (await _entryRepo.Query().AnyAsync(e => e.WastageReasonId == cmd.Id, ct))
            return ApiResponse.Fail("This reason is used by wastage entries (deactivate it instead).");
        _repo.Remove(r);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Wastage reason deleted.");
    }
}
