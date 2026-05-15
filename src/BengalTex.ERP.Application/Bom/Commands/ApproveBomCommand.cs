using BengalTex.ERP.Application.Bom.Dtos;
using BengalTex.ERP.Application.Bom.Queries;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Bom.Commands;

/// <summary>
/// Approves a draft BOM and makes it the active version for its product.
/// The previously active version is archived. Two-phase save: the demote is
/// committed before the promote so the filtered unique index never sees two
/// active rows for the same product.
/// </summary>
public sealed record ApproveBomCommand(int Id) : IRequest<ApiResponse<BomDto>>;

internal sealed class ApproveBomCommandHandler
    : IRequestHandler<ApproveBomCommand, ApiResponse<BomDto>>
{
    private readonly IRepository<Domain.Entities.Bom> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public ApproveBomCommandHandler(
        IRepository<Domain.Entities.Bom> repo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<BomDto>> Handle(
        ApproveBomCommand cmd, CancellationToken cancellationToken)
    {
        var bom = await _repo.Query()
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == cmd.Id, cancellationToken);

        if (bom is null) return ApiResponse<BomDto>.Fail("BOM not found.");
        if (bom.Status != Domain.Entities.BomStatus.Draft)
            return ApiResponse<BomDto>.Fail("Only draft BOMs can be approved.");
        if (bom.Lines.Count == 0)
            return ApiResponse<BomDto>.Fail("Cannot approve a BOM with no lines.");

        // Phase 1 — demote the currently active version FIRST and commit separately
        var currentActive = await _repo.Query()
            .FirstOrDefaultAsync(
                b => b.ProductId == bom.ProductId && b.IsActive && b.Id != bom.Id,
                cancellationToken);
        if (currentActive is not null)
        {
            currentActive.IsActive = false;
            currentActive.Status = Domain.Entities.BomStatus.Archived;
            _repo.Update(currentActive);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        // Phase 2 — promote this BOM
        bom.Status = Domain.Entities.BomStatus.Approved;
        bom.IsActive = true;
        bom.ApprovedAt = DateTimeOffset.UtcNow;
        bom.ApprovedBy = _currentUser.UserName;
        _repo.Update(bom);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetBomByIdQuery(bom.Id), cancellationToken);
    }
}
