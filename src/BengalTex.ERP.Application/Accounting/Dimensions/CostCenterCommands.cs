using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Dimensions;

// ═══════════════════════════ DTO ═══════════════════════════

public sealed record CostCenterDto(
    int Id, string Code, string Name, string Kind,
    int? ParentCostCenterId, string? ParentName,
    int? DepartmentId, string? DepartmentName,
    int? FactoryId, string? FactoryName,
    bool IsActive, string? Description);

// ═══════════════════════════ Queries ═══════════════════════════

public sealed record GetCostCentersQuery(bool IncludeInactive = false)
    : IRequest<ApiResponse<IReadOnlyList<CostCenterDto>>>;

internal sealed class GetCostCentersQueryHandler
    : IRequestHandler<GetCostCentersQuery, ApiResponse<IReadOnlyList<CostCenterDto>>>
{
    private readonly IRepository<CostCenter> _repo;
    public GetCostCentersQueryHandler(IRepository<CostCenter> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<CostCenterDto>>> Handle(GetCostCentersQuery q, CancellationToken ct)
    {
        IQueryable<CostCenter> query = _repo.Query().AsNoTracking()
            .Include(c => c.ParentCostCenter).Include(c => c.Department).Include(c => c.Factory);
        if (!q.IncludeInactive) query = query.Where(c => c.IsActive);

        var rows = await query.OrderBy(c => c.Code).Select(c => new CostCenterDto(
            c.Id, c.Code, c.Name, c.Kind.ToString(),
            c.ParentCostCenterId, c.ParentCostCenter != null ? c.ParentCostCenter.Name : null,
            c.DepartmentId, c.Department != null ? c.Department.Name : null,
            c.FactoryId, c.Factory != null ? c.Factory.Name : null,
            c.IsActive, c.Description)).ToListAsync(ct);

        return ApiResponse<IReadOnlyList<CostCenterDto>>.Ok(rows);
    }
}

// ═══════════════════════════ Create ═══════════════════════════

public sealed record CreateCostCenterCommand(
    string Code, string Name, string Kind, int? ParentCostCenterId,
    int? DepartmentId, int? FactoryId, string? Description) : IRequest<ApiResponse<int>>;

public sealed class CreateCostCenterCommandValidator : AbstractValidator<CreateCostCenterCommand>
{
    public CreateCostCenterCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Kind).Must(k => Enum.TryParse<CostCenterKind>(k, out _))
            .WithMessage("Kind must be Cost, Profit or Both.");
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class CreateCostCenterCommandHandler : IRequestHandler<CreateCostCenterCommand, ApiResponse<int>>
{
    private readonly IRepository<CostCenter> _repo;
    private readonly IUnitOfWork _uow;
    public CreateCostCenterCommandHandler(IRepository<CostCenter> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateCostCenterCommand cmd, CancellationToken ct)
    {
        if (await _repo.AnyAsync(c => c.Code == cmd.Code.Trim(), ct))
            return ApiResponse<int>.Fail($"Cost center code '{cmd.Code}' already exists.");

        var cc = new CostCenter
        {
            Code = cmd.Code.Trim(),
            Name = cmd.Name.Trim(),
            Kind = Enum.Parse<CostCenterKind>(cmd.Kind),
            ParentCostCenterId = cmd.ParentCostCenterId,
            DepartmentId = cmd.DepartmentId,
            FactoryId = cmd.FactoryId,
            Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim(),
            IsActive = true
        };
        await _repo.AddAsync(cc, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(cc.Id, $"Cost center {cc.Code} created.");
    }
}

// ═══════════════════════════ Update ═══════════════════════════

public sealed record UpdateCostCenterCommand(
    int Id, string Name, string Kind, int? ParentCostCenterId,
    int? DepartmentId, int? FactoryId, bool IsActive, string? Description) : IRequest<ApiResponse>;

public sealed class UpdateCostCenterCommandValidator : AbstractValidator<UpdateCostCenterCommand>
{
    public UpdateCostCenterCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Kind).Must(k => Enum.TryParse<CostCenterKind>(k, out _));
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class UpdateCostCenterCommandHandler : IRequestHandler<UpdateCostCenterCommand, ApiResponse>
{
    private readonly IRepository<CostCenter> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateCostCenterCommandHandler(IRepository<CostCenter> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(UpdateCostCenterCommand cmd, CancellationToken ct)
    {
        var cc = await _repo.GetByIdAsync(cmd.Id, ct);
        if (cc is null) return ApiResponse.Fail("Cost center not found.");
        if (cmd.ParentCostCenterId == cmd.Id) return ApiResponse.Fail("A cost center cannot be its own parent.");

        cc.Name = cmd.Name.Trim();
        cc.Kind = Enum.Parse<CostCenterKind>(cmd.Kind);
        cc.ParentCostCenterId = cmd.ParentCostCenterId;
        cc.DepartmentId = cmd.DepartmentId;
        cc.FactoryId = cmd.FactoryId;
        cc.IsActive = cmd.IsActive;
        cc.Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();
        _repo.Update(cc);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Cost center updated.");
    }
}
