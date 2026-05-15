using BengalTex.ERP.Application.Bom.Dtos;
using BengalTex.ERP.Application.Bom.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Bom.Commands;

/// <summary>
/// Makes an already-approved (or archived) BOM the active version for its product —
/// e.g. rolling back to an earlier spec. The currently active version is archived.
/// Same two-phase save as <see cref="ApproveBomCommand"/>.
/// </summary>
public sealed record ActivateBomCommand(int Id) : IRequest<ApiResponse<BomDto>>;

internal sealed class ActivateBomCommandHandler
    : IRequestHandler<ActivateBomCommand, ApiResponse<BomDto>>
{
    private readonly IRepository<Domain.Entities.Bom> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public ActivateBomCommandHandler(
        IRepository<Domain.Entities.Bom> repo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<BomDto>> Handle(
        ActivateBomCommand cmd, CancellationToken cancellationToken)
    {
        var bom = await _repo.GetByIdAsync(cmd.Id, cancellationToken);

        if (bom is null) return ApiResponse<BomDto>.Fail("BOM not found.");
        if (bom.Status == Domain.Entities.BomStatus.Draft)
            return ApiResponse<BomDto>.Fail("Draft BOMs must be approved, not activated.");
        if (bom.IsActive)
            return ApiResponse<BomDto>.Fail("This BOM is already the active version.");

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

        // Phase 2 — promote this BOM back to active
        bom.Status = Domain.Entities.BomStatus.Approved;
        bom.IsActive = true;
        _repo.Update(bom);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetBomByIdQuery(bom.Id), cancellationToken);
    }
}
