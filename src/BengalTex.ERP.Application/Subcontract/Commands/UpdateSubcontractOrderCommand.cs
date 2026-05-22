using BengalTex.ERP.Application.Subcontract.Dtos;
using BengalTex.ERP.Application.Subcontract.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Subcontract.Commands;

public sealed record UpdateSubcontractOrderCommand(
    long Id,
    int SubcontractorId,
    DateOnly OrderDate,
    DateOnly? ExpectedReturnDate,
    string ProcessType,
    int WarehouseId,
    decimal ChargeAmount,
    string? Notes,
    IReadOnlyList<SubcontractLineInput> Lines
) : IRequest<ApiResponse<SubcontractOrderDto>>;

public sealed class UpdateSubcontractOrderCommandValidator : AbstractValidator<UpdateSubcontractOrderCommand>
{
    public UpdateSubcontractOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.SubcontractorId).GreaterThan(0);
        RuleFor(x => x.OrderDate).NotEmpty();
        RuleFor(x => x.ProcessType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.ChargeAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l).Must(l => (l.RawMaterialId.HasValue) ^ (l.ProductId.HasValue))
                .WithMessage("Each line must reference exactly one item (raw material OR product).");
            line.RuleFor(l => l.IssuedQuantity).GreaterThan(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
    }
}

internal sealed class UpdateSubcontractOrderCommandHandler
    : IRequestHandler<UpdateSubcontractOrderCommand, ApiResponse<SubcontractOrderDto>>
{
    private readonly IRepository<SubcontractOrder, long> _repo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateSubcontractOrderCommandHandler(
        IRepository<SubcontractOrder, long> repo, IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow, IMediator mediator)
    {
        _repo = repo;
        _warehouseRepo = warehouseRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SubcontractOrderDto>> Handle(
        UpdateSubcontractOrderCommand cmd, CancellationToken ct)
    {
        var order = await _repo.Query()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, ct);
        if (order is null) return ApiResponse<SubcontractOrderDto>.Fail("Subcontract order not found.");
        if (order.Status != SubcontractStatus.Draft)
            return ApiResponse<SubcontractOrderDto>.Fail("Only draft subcontract orders can be edited.");
        if (await _warehouseRepo.GetByIdAsync(cmd.WarehouseId, ct) is null)
            return ApiResponse<SubcontractOrderDto>.Fail("Warehouse not found.");

        order.SubcontractorId = cmd.SubcontractorId;
        order.OrderDate = cmd.OrderDate;
        order.ExpectedReturnDate = cmd.ExpectedReturnDate;
        order.ProcessType = cmd.ProcessType.Trim();
        order.WarehouseId = cmd.WarehouseId;
        order.ChargeAmount = cmd.ChargeAmount;
        order.Notes = cmd.Notes;

        order.Lines.Clear();
        var sortOrder = 0;
        foreach (var l in cmd.Lines)
        {
            order.Lines.Add(new SubcontractLine
            {
                RawMaterialId = l.RawMaterialId,
                ProductId = l.ProductId,
                IssuedQuantity = l.IssuedQuantity,
                ReceivedQuantity = 0m,
                SortOrder = sortOrder++,
                LineNotes = l.LineNotes
            });
        }

        _repo.Update(order);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetSubcontractOrderByIdQuery(order.Id), ct);
    }
}
