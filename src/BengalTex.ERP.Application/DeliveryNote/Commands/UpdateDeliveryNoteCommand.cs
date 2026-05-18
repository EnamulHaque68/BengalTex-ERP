using BengalTex.ERP.Application.DeliveryNote.Dtos;
using BengalTex.ERP.Application.DeliveryNote.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.DeliveryNote.Commands;

public sealed record UpdateDeliveryNoteCommand(
    long Id,
    DateOnly DispatchDate,
    int DispatchWarehouseId,
    string? VehicleNumber,
    string? DriverContact,
    string? DeliveryAddress,
    string? Notes,
    IReadOnlyList<DeliveryNoteLineInput> Lines
) : IRequest<ApiResponse<DeliveryNoteDto>>;

public sealed class UpdateDeliveryNoteCommandValidator : AbstractValidator<UpdateDeliveryNoteCommand>
{
    public UpdateDeliveryNoteCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.DispatchDate).NotEmpty();
        RuleFor(x => x.DispatchWarehouseId).GreaterThan(0);
        RuleFor(x => x.VehicleNumber).MaximumLength(50);
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

internal sealed class UpdateDeliveryNoteCommandHandler
    : IRequestHandler<UpdateDeliveryNoteCommand, ApiResponse<DeliveryNoteDto>>
{
    private readonly IRepository<Domain.Entities.DeliveryNote, long> _repo;
    private readonly IRepository<Domain.Entities.SalesOrder, long> _soRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateDeliveryNoteCommandHandler(
        IRepository<Domain.Entities.DeliveryNote, long> repo,
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _soRepo = soRepo;
        _warehouseRepo = warehouseRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<DeliveryNoteDto>> Handle(
        UpdateDeliveryNoteCommand cmd, CancellationToken cancellationToken)
    {
        var dn = await _repo.Query()
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == cmd.Id, cancellationToken);

        if (dn is null) return ApiResponse<DeliveryNoteDto>.Fail("Delivery note not found.");
        if (dn.Status != Domain.Entities.DeliveryNoteStatus.Draft)
            return ApiResponse<DeliveryNoteDto>.Fail("Only draft delivery notes can be edited.");

        var warehouse = await _warehouseRepo.GetByIdAsync(cmd.DispatchWarehouseId, cancellationToken);
        if (warehouse is null)
            return ApiResponse<DeliveryNoteDto>.Fail("Dispatch warehouse not found.");

        var so = await _soRepo.Query()
            .Include(s => s.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dn.SalesOrderId, cancellationToken);
        if (so is null) return ApiResponse<DeliveryNoteDto>.Fail("Parent sales order not found.");

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

        dn.DispatchDate = cmd.DispatchDate;
        dn.DispatchWarehouseId = cmd.DispatchWarehouseId;
        dn.VehicleNumber = string.IsNullOrWhiteSpace(cmd.VehicleNumber) ? null : cmd.VehicleNumber.Trim();
        dn.DriverContact = string.IsNullOrWhiteSpace(cmd.DriverContact) ? null : cmd.DriverContact.Trim();
        dn.DeliveryAddress = string.IsNullOrWhiteSpace(cmd.DeliveryAddress) ? null : cmd.DeliveryAddress.Trim();
        dn.Notes = cmd.Notes;

        dn.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            dn.Lines.Add(new Domain.Entities.DeliveryNoteLine
            {
                SalesOrderLineId = line.SalesOrderLineId,
                DispatchedQuantity = line.DispatchedQuantity,
                SortOrder = sortOrder++,
                LineNotes = line.LineNotes
            });
        }

        _repo.Update(dn);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetDeliveryNoteByIdQuery(dn.Id), cancellationToken);
    }
}
