using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Application.Production.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Production.Commands;

/// <summary>
/// Marks a routing stage InProgress. Sequential routing: every earlier stage must already be
/// Completed/Skipped. Allowed only while the parent order is InProgress.
/// </summary>
public sealed record StartProductionStageCommand(long StageId) : IRequest<ApiResponse<ProductionOrderDto>>;

/// <summary>Marks a routing stage Skipped (not applicable to this run).</summary>
public sealed record SkipProductionStageCommand(long StageId) : IRequest<ApiResponse<ProductionOrderDto>>;

internal sealed class StartProductionStageCommandHandler
    : IRequestHandler<StartProductionStageCommand, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionStage, long> _stageRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public StartProductionStageCommandHandler(
        IRepository<Domain.Entities.ProductionStage, long> stageRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _stageRepo = stageRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        StartProductionStageCommand cmd, CancellationToken cancellationToken)
    {
        var stage = await _stageRepo.Query()
            .Include(s => s.ProductionOrder)
            .FirstOrDefaultAsync(s => s.Id == cmd.StageId, cancellationToken);

        if (stage is null) return ApiResponse<ProductionOrderDto>.Fail("Production stage not found.");
        if (stage.ProductionOrder.Status != Domain.Entities.ProductionOrderStatus.InProgress)
            return ApiResponse<ProductionOrderDto>.Fail("The production order must be in progress to start a stage.");
        if (stage.Status != Domain.Entities.ProductionStageStatus.Pending)
            return ApiResponse<ProductionOrderDto>.Fail($"This stage is already {stage.Status}.");

        // Sequential routing — earlier stages must be done first.
        var earlierUnfinished = await _stageRepo.Query()
            .AnyAsync(s => s.ProductionOrderId == stage.ProductionOrderId
                        && s.Sequence < stage.Sequence
                        && s.Status != Domain.Entities.ProductionStageStatus.Completed
                        && s.Status != Domain.Entities.ProductionStageStatus.Skipped,
                cancellationToken);
        if (earlierUnfinished)
            return ApiResponse<ProductionOrderDto>.Fail("Complete or skip the earlier stages first.");

        stage.Status = Domain.Entities.ProductionStageStatus.InProgress;
        stage.StartedAt = DateTimeOffset.UtcNow;
        _stageRepo.Update(stage);

        await _uow.SaveChangesAsync(cancellationToken);
        return await _mediator.Send(new GetProductionOrderByIdQuery(stage.ProductionOrderId), cancellationToken);
    }
}

internal sealed class SkipProductionStageCommandHandler
    : IRequestHandler<SkipProductionStageCommand, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionStage, long> _stageRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public SkipProductionStageCommandHandler(
        IRepository<Domain.Entities.ProductionStage, long> stageRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _stageRepo = stageRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        SkipProductionStageCommand cmd, CancellationToken cancellationToken)
    {
        var stage = await _stageRepo.Query()
            .Include(s => s.ProductionOrder)
            .FirstOrDefaultAsync(s => s.Id == cmd.StageId, cancellationToken);

        if (stage is null) return ApiResponse<ProductionOrderDto>.Fail("Production stage not found.");
        if (stage.ProductionOrder.Status != Domain.Entities.ProductionOrderStatus.InProgress)
            return ApiResponse<ProductionOrderDto>.Fail("The production order must be in progress to skip a stage.");
        if (stage.Status is not (Domain.Entities.ProductionStageStatus.Pending
                              or Domain.Entities.ProductionStageStatus.InProgress))
            return ApiResponse<ProductionOrderDto>.Fail($"This stage is already {stage.Status}.");

        stage.Status = Domain.Entities.ProductionStageStatus.Skipped;
        stage.CompletedAt = DateTimeOffset.UtcNow;
        _stageRepo.Update(stage);

        await _uow.SaveChangesAsync(cancellationToken);
        return await _mediator.Send(new GetProductionOrderByIdQuery(stage.ProductionOrderId), cancellationToken);
    }
}
