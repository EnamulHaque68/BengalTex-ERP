using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Application.Production.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Production.Commands;

public sealed record CreateProductionOrderCommand(
    int ProductId,
    int BomId,
    decimal Quantity,
    int IssueWarehouseId,
    int ReceiveWarehouseId,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    string? Notes
) : IRequest<ApiResponse<ProductionOrderDto>>;

public sealed class CreateProductionOrderCommandValidator : AbstractValidator<CreateProductionOrderCommand>
{
    public CreateProductionOrderCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.BomId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.IssueWarehouseId).GreaterThan(0);
        RuleFor(x => x.ReceiveWarehouseId).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateProductionOrderCommandHandler
    : IRequestHandler<CreateProductionOrderCommand, ApiResponse<ProductionOrderDto>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _repo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IRepository<Domain.Entities.Bom> _bomRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateProductionOrderCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> repo,
        IRepository<Domain.Entities.Product> productRepo,
        IRepository<Domain.Entities.Bom> bomRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _productRepo = productRepo;
        _bomRepo = bomRepo;
        _warehouseRepo = warehouseRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<ProductionOrderDto>> Handle(
        CreateProductionOrderCommand cmd, CancellationToken cancellationToken)
    {
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

        var code = await _numbering.NextAsync("PRD", null, cancellationToken);

        var entity = new Domain.Entities.ProductionOrder
        {
            Code = code,
            ProductId = cmd.ProductId,
            BomId = cmd.BomId,
            Quantity = cmd.Quantity,
            IssueWarehouseId = cmd.IssueWarehouseId,
            ReceiveWarehouseId = cmd.ReceiveWarehouseId,
            PlannedStartDate = cmd.PlannedStartDate,
            PlannedEndDate = cmd.PlannedEndDate,
            Status = Domain.Entities.ProductionOrderStatus.Draft,
            Notes = cmd.Notes
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetProductionOrderByIdQuery(entity.Id), cancellationToken);
    }
}
