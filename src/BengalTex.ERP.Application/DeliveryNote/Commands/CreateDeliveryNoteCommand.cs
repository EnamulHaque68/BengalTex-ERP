using BengalTex.ERP.Application.DeliveryNote.Dtos;
using BengalTex.ERP.Application.DeliveryNote.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.DeliveryNote.Commands;

/// <summary>One product line submitted with a create/update DN request.</summary>
public sealed record DeliveryNoteLineInput(
    long SalesOrderLineId,
    decimal DispatchedQuantity,
    string? LineNotes);

public sealed record CreateDeliveryNoteCommand(
    long SalesOrderId,
    DateOnly DispatchDate,
    int DispatchWarehouseId,
    string? VehicleNumber,
    string? DriverContact,
    string? DeliveryAddress,
    string? Notes,
    IReadOnlyList<DeliveryNoteLineInput> Lines,
    DateOnly? PlannedDeliveryDate = null,
    string? TransportCompany = null,
    string? DriverName = null
) : IRequest<ApiResponse<DeliveryNoteDto>>;

public sealed class CreateDeliveryNoteCommandValidator : AbstractValidator<CreateDeliveryNoteCommand>
{
    public CreateDeliveryNoteCommandValidator()
    {
        RuleFor(x => x.SalesOrderId).GreaterThan(0);
        RuleFor(x => x.DispatchDate).NotEmpty();
        RuleFor(x => x.DispatchWarehouseId).GreaterThan(0);
        RuleFor(x => x.VehicleNumber).MaximumLength(50);
        RuleFor(x => x.TransportCompany).MaximumLength(150);
        RuleFor(x => x.DriverName).MaximumLength(100);
        RuleFor(x => x.DriverContact).MaximumLength(100);
        RuleFor(x => x.DeliveryAddress).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A delivery note must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.SalesOrderLineId).GreaterThan(0);
            line.RuleFor(l => l.DispatchedQuantity).GreaterThan(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.SalesOrderLineId).Distinct().Count() == lines.Count)
            .WithMessage("The same SO line appears more than once — combine the quantities.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class CreateDeliveryNoteCommandHandler
    : IRequestHandler<CreateDeliveryNoteCommand, ApiResponse<DeliveryNoteDto>>
{
    private readonly IRepository<Domain.Entities.DeliveryNote, long> _repo;
    private readonly IRepository<Domain.Entities.SalesOrder, long> _soRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateDeliveryNoteCommandHandler(
        IRepository<Domain.Entities.DeliveryNote, long> repo,
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _soRepo = soRepo;
        _warehouseRepo = warehouseRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<DeliveryNoteDto>> Handle(
        CreateDeliveryNoteCommand cmd, CancellationToken cancellationToken)
    {
        var so = await _soRepo.Query()
            .Include(s => s.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == cmd.SalesOrderId, cancellationToken);
        if (so is null) return ApiResponse<DeliveryNoteDto>.Fail("Sales order not found.");

        if (so.Status != Domain.Entities.SalesOrderStatus.Confirmed &&
            so.Status != Domain.Entities.SalesOrderStatus.PartiallyDispatched)
        {
            return ApiResponse<DeliveryNoteDto>.Fail(
                "Goods can only be dispatched against a Confirmed or partially-dispatched sales order.");
        }

        var warehouse = await _warehouseRepo.GetByIdAsync(cmd.DispatchWarehouseId, cancellationToken);
        if (warehouse is null)
            return ApiResponse<DeliveryNoteDto>.Fail("Dispatch warehouse not found.");

        // Each DN line must reference a line of this SO and not over-dispatch against remaining
        foreach (var line in cmd.Lines)
        {
            var soLine = so.Lines.FirstOrDefault(sl => sl.Id == line.SalesOrderLineId);
            if (soLine is null)
                return ApiResponse<DeliveryNoteDto>.Fail(
                    $"SO line {line.SalesOrderLineId} does not belong to SO {so.Code}.");

            var remaining = soLine.Quantity - soLine.DispatchedQuantity;
            if (line.DispatchedQuantity > remaining)
                return ApiResponse<DeliveryNoteDto>.Fail(
                    $"SO line {soLine.Id}: would exceed ordered qty ({remaining:0.####} remaining).");
        }

        var code = await _numbering.NextAsync("DN", null, cancellationToken);

        var entity = new Domain.Entities.DeliveryNote
        {
            Code = code,
            SalesOrderId = cmd.SalesOrderId,
            DispatchDate = cmd.DispatchDate,
            DispatchWarehouseId = cmd.DispatchWarehouseId,
            Status = Domain.Entities.DeliveryNoteStatus.Draft,
            PlannedDeliveryDate = cmd.PlannedDeliveryDate,
            VehicleNumber = string.IsNullOrWhiteSpace(cmd.VehicleNumber) ? null : cmd.VehicleNumber.Trim(),
            TransportCompany = string.IsNullOrWhiteSpace(cmd.TransportCompany) ? null : cmd.TransportCompany.Trim(),
            DriverName = string.IsNullOrWhiteSpace(cmd.DriverName) ? null : cmd.DriverName.Trim(),
            DriverContact = string.IsNullOrWhiteSpace(cmd.DriverContact) ? null : cmd.DriverContact.Trim(),
            DeliveryAddress = string.IsNullOrWhiteSpace(cmd.DeliveryAddress) ? null : cmd.DeliveryAddress.Trim(),
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) => new Domain.Entities.DeliveryNoteLine
            {
                SalesOrderLineId = l.SalesOrderLineId,
                DispatchedQuantity = l.DispatchedQuantity,
                SortOrder = i,
                LineNotes = l.LineNotes
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetDeliveryNoteByIdQuery(entity.Id), cancellationToken);
    }
}
