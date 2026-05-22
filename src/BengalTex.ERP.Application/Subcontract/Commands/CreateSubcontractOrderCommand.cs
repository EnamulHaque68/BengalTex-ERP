using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.Subcontract.Dtos;
using BengalTex.ERP.Application.Subcontract.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Subcontract.Commands;

/// <summary>One polymorphic line (RawMaterial XOR Product) on a create/update request.</summary>
public sealed record SubcontractLineInput(
    int? RawMaterialId,
    int? ProductId,
    decimal IssuedQuantity,
    string? LineNotes);

public sealed record CreateSubcontractOrderCommand(
    int SubcontractorId,
    DateOnly OrderDate,
    DateOnly? ExpectedReturnDate,
    string ProcessType,
    int WarehouseId,
    decimal ChargeAmount,
    string? Notes,
    IReadOnlyList<SubcontractLineInput> Lines
) : IRequest<ApiResponse<SubcontractOrderDto>>;

public sealed class CreateSubcontractOrderCommandValidator : AbstractValidator<CreateSubcontractOrderCommand>
{
    public CreateSubcontractOrderCommandValidator()
    {
        RuleFor(x => x.SubcontractorId).GreaterThan(0);
        RuleFor(x => x.OrderDate).NotEmpty();
        RuleFor(x => x.ProcessType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.ChargeAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A subcontract order must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l).Must(l => (l.RawMaterialId.HasValue) ^ (l.ProductId.HasValue))
                .WithMessage("Each line must reference exactly one item (raw material OR product).");
            line.RuleFor(l => l.IssuedQuantity).GreaterThan(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
    }
}

internal sealed class CreateSubcontractOrderCommandHandler
    : IRequestHandler<CreateSubcontractOrderCommand, ApiResponse<SubcontractOrderDto>>
{
    private readonly IRepository<SubcontractOrder, long> _repo;
    private readonly IRepository<Domain.Entities.Supplier> _supplierRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rawMaterialRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateSubcontractOrderCommandHandler(
        IRepository<SubcontractOrder, long> repo,
        IRepository<Domain.Entities.Supplier> supplierRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IRepository<Domain.Entities.RawMaterial> rawMaterialRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _supplierRepo = supplierRepo;
        _warehouseRepo = warehouseRepo;
        _rawMaterialRepo = rawMaterialRepo;
        _productRepo = productRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SubcontractOrderDto>> Handle(
        CreateSubcontractOrderCommand cmd, CancellationToken ct)
    {
        if (await _supplierRepo.GetByIdAsync(cmd.SubcontractorId, ct) is null)
            return ApiResponse<SubcontractOrderDto>.Fail("Subcontractor (supplier) not found.");
        if (await _warehouseRepo.GetByIdAsync(cmd.WarehouseId, ct) is null)
            return ApiResponse<SubcontractOrderDto>.Fail("Warehouse not found.");

        var rmIds = cmd.Lines.Where(l => l.RawMaterialId.HasValue).Select(l => l.RawMaterialId!.Value).Distinct().ToList();
        var prodIds = cmd.Lines.Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToList();
        if (rmIds.Count > 0 && await _rawMaterialRepo.Query().CountAsync(r => rmIds.Contains(r.Id), ct) != rmIds.Count)
            return ApiResponse<SubcontractOrderDto>.Fail("One or more raw materials not found.");
        if (prodIds.Count > 0 && await _productRepo.Query().CountAsync(p => prodIds.Contains(p.Id), ct) != prodIds.Count)
            return ApiResponse<SubcontractOrderDto>.Fail("One or more products not found.");

        var code = await _numbering.NextAsync("SUB", null, ct);

        var entity = new SubcontractOrder
        {
            Code = code,
            SubcontractorId = cmd.SubcontractorId,
            OrderDate = cmd.OrderDate,
            ExpectedReturnDate = cmd.ExpectedReturnDate,
            ProcessType = cmd.ProcessType.Trim(),
            WarehouseId = cmd.WarehouseId,
            Status = SubcontractStatus.Draft,
            ChargeAmount = cmd.ChargeAmount,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new SubcontractLine
            {
                RawMaterialId = l.RawMaterialId,
                ProductId = l.ProductId,
                IssuedQuantity = l.IssuedQuantity,
                ReceivedQuantity = 0m,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };

        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetSubcontractOrderByIdQuery(entity.Id), ct);
    }
}
