using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Application.Production.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Production.Commands;

/// <summary>
/// Marks a routing stage Completed, recording the good + rejected quantities that passed
/// through it. Allowed while the parent order is InProgress, on a Pending or InProgress stage.
/// Tracking only — no stock movement (stock still posts at order Complete).
/// </summary>
public sealed record CompleteProductionStageCommand(
    long StageId,
    decimal CompletedQuantity,
    decimal RejectedQuantity,
    string? Notes
) : IRequest<ApiResponse<ProductionOrderDto>>;

public sealed class CompleteProductionStageCommandValidator : AbstractValidator<CompleteProductionStageCommand>
{
    public CompleteProductionStageCommandValidator()
    {
        RuleFor(x => x.StageId).GreaterThan(0);
        RuleFor(x => x.CompletedQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RejectedQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CompleteProductionStageCommandHandler
    : IRequestHandler<CompleteProductionStageCommand, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionStage, long> _stageRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public CompleteProductionStageCommandHandler(
        IRepository<Domain.Entities.ProductionStage, long> stageRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _stageRepo = stageRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        CompleteProductionStageCommand cmd, CancellationToken cancellationToken)
    {
        var stage = await _stageRepo.Query()
            .Include(s => s.ProductionOrder)
            .FirstOrDefaultAsync(s => s.Id == cmd.StageId, cancellationToken);

        if (stage is null) return ApiResponse<ProductionOrderDto>.Fail("Production stage not found.");
        if (stage.ProductionOrder.Status != Domain.Entities.ProductionOrderStatus.InProgress)
            return ApiResponse<ProductionOrderDto>.Fail("The production order must be in progress to update its stages.");
        if (stage.Status is not (Domain.Entities.ProductionStageStatus.Pending
                              or Domain.Entities.ProductionStageStatus.InProgress))
            return ApiResponse<ProductionOrderDto>.Fail($"This stage is already {stage.Status}.");

        var now = DateTimeOffset.UtcNow;
        stage.CompletedQuantity = cmd.CompletedQuantity;
        stage.RejectedQuantity = cmd.RejectedQuantity;
        stage.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        stage.Status = Domain.Entities.ProductionStageStatus.Completed;
        stage.StartedAt ??= now;
        stage.CompletedAt = now;
        _stageRepo.Update(stage);

        await _uow.SaveChangesAsync(cancellationToken);
        return await _mediator.Send(new GetProductionOrderByIdQuery(stage.ProductionOrderId), cancellationToken);
    }
}
