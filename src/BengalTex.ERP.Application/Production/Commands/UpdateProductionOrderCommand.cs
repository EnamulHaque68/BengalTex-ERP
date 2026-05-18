using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Application.Production.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Production.Commands;

public sealed record UpdateProductionOrderCommand(
    long Id,
    int ProductId,
    int BomId,
    decimal Quantity,
    int IssueWarehouseId,
    int ReceiveWarehouseId,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    string? Notes
) : IRequest<ApiResponse<ProductionOrderDto>>;

public sealed class UpdateProductionOrderCommandValidator : AbstractValidator<UpdateProductionOrderCommand>
{
    public UpdateProductionOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.BomId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.IssueWarehouseId).GreaterThan(0);
        RuleFor(x => x.ReceiveWarehouseId).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateProductionOrderCommandHandler
    : IRequestHandler<UpdateProductionOrderCommand, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IRepository<Domain.Entities.Bom> _bomRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateProductionOrderCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IRepository<Domain.Entities.Product> productRepo,
        IRepository<Domain.Entities.Bom> bomRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _productRepo = productRepo;
        _bomRepo = bomRepo;
        _warehouseRepo = warehouseRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        UpdateProductionOrderCommand cmd, CancellationToken cancellationToken)
    {
        var po = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (po is null) return ApiResponse<ProductionOrderDto>.Fail("Production order not found.");
        if (po.Status != Domain.Entities.ProductionOrderStatus.Draft)
            return ApiResponse<ProductionOrderDto>.Fail("Only draft production orders can be edited.");

        var product = await _productRepo.GetByIdAsync(cmd.ProductId, cancellationToken);
        if (product is null) return ApiResponse<ProductionOrderDto>.Fail("Product not found.");

        var bom = await _bomRepo.GetByIdAsync(cmd.BomId, cancellationToken);
        if (bom is null) return ApiResponse<ProductionOrderDto>.Fail("BOM not found.");
        if (bom.ProductId != cmd.ProductId)
            return ApiResponse<ProductionOrderDto>.Fail("BOM does not belong to the selected product.");

        var issueWh = await _warehouseRepo.GetByIdAsync(cmd.IssueWarehouseId, cancellationToken);
        if (issueWh is null) return ApiResponse<ProductionOrderDto>.Fail("Issue warehouse not found.");

        var receiveWh = await _warehouseRepo.GetByIdAsync(cmd.ReceiveWarehouseId, cancellationToken);
        if (receiveWh is null) return ApiResponse<ProductionOrderDto>.Fail("Receive warehouse not found.");

        po.ProductId = cmd.ProductId;
        po.BomId = cmd.BomId;
        po.Quantity = cmd.Quantity;
        po.IssueWarehouseId = cmd.IssueWarehouseId;
        po.ReceiveWarehouseId = cmd.ReceiveWarehouseId;
        po.PlannedStartDate = cmd.PlannedStartDate;
        po.PlannedEndDate = cmd.PlannedEndDate;
        po.Notes = cmd.Notes;

        _repo.Update(po);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetProductionOrderByIdQuery(po.Id), cancellationToken);
    }
}
