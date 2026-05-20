using BengalTex.ERP.Application.CustomerReturnNote.Dtos;
using BengalTex.ERP.Application.CustomerReturnNote.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerReturnNote.Commands;

/// <summary>One line submitted with a create/update Customer Return Note request.</summary>
public sealed record CustomerReturnNoteLineInput(
    long DeliveryNoteLineId,
    decimal ReturnedQuantity,
    string? LineNotes);

/// <summary>
/// Creates a Draft Customer Return Note against a previously posted Delivery Note.
/// No stock movement yet — Posting is a separate step. Lines must reference DN
/// lines that belong to the named DN; ReturnedQuantity must be &gt; 0 and ≤
/// (DispatchedQty − PreviouslyReturnedQty) per line.
/// </summary>
public sealed record CreateCustomerReturnNoteCommand(
    long DeliveryNoteId,
    int ReturnWarehouseId,
    DateOnly ReturnDate,
    string? VehicleNumber,
    string? Reason,
    string? Notes,
    IReadOnlyList<CustomerReturnNoteLineInput> Lines
) : IRequest<ApiResponse<CustomerReturnNoteDto>>;

public sealed class CreateCustomerReturnNoteCommandValidator : AbstractValidator<CreateCustomerReturnNoteCommand>
{
    public CreateCustomerReturnNoteCommandValidator()
    {
        RuleFor(x => x.DeliveryNoteId).GreaterThan(0);
        RuleFor(x => x.ReturnWarehouseId).GreaterThan(0);
        RuleFor(x => x.ReturnDate).NotEmpty();
        RuleFor(x => x.VehicleNumber).MaximumLength(50);
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A customer return note must have at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.DeliveryNoteLineId).GreaterThan(0);
            line.RuleFor(l => l.ReturnedQuantity).GreaterThan(0);
            line.RuleFor(l => l.LineNotes).MaximumLength(1000);
        });
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.DeliveryNoteLineId).Distinct().Count() == lines.Count)
            .WithMessage("The same delivery-note line appears more than once.")
            .When(x => x.Lines is { Count: > 0 });
    }
}

internal sealed class CreateCustomerReturnNoteCommandHandler
    : IRequestHandler<CreateCustomerReturnNoteCommand, ApiResponse<CustomerReturnNoteDto>>
{
    private readonly IRepository<Domain.Entities.CustomerReturnNote, long> _repo;
    private readonly IRepository<Domain.Entities.DeliveryNote, long> _dnRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateCustomerReturnNoteCommandHandler(
        IRepository<Domain.Entities.CustomerReturnNote, long> repo,
        IRepository<Domain.Entities.DeliveryNote, long> dnRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _dnRepo = dnRepo;
        _warehouseRepo = warehouseRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerReturnNoteDto>> Handle(
        CreateCustomerReturnNoteCommand cmd, CancellationToken cancellationToken)
    {
        var dn = await _dnRepo.Query()
            .Include(d => d.Lines).ThenInclude(l => l.SalesOrderLine).ThenInclude(sl => sl.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == cmd.DeliveryNoteId, cancellationToken);

        if (dn is null) return ApiResponse<CustomerReturnNoteDto>.Fail("Delivery note not found.");
        if (dn.Status != Domain.Entities.DeliveryNoteStatus.Posted)
            return ApiResponse<CustomerReturnNoteDto>.Fail("Customer returns can only be recorded against a Posted delivery note.");

        var warehouse = await _warehouseRepo.GetByIdAsync(cmd.ReturnWarehouseId, cancellationToken);
        if (warehouse is null) return ApiResponse<CustomerReturnNoteDto>.Fail("Return warehouse not found.");

        // Per-line validation — each input must match a DN line on this DN + qty within available
        var dnLineById = dn.Lines.ToDictionary(l => l.Id);
        foreach (var input in cmd.Lines)
        {
            if (!dnLineById.TryGetValue(input.DeliveryNoteLineId, out var dnLine))
                return ApiResponse<CustomerReturnNoteDto>.Fail(
                    $"Delivery-note line {input.DeliveryNoteLineId} does not belong to DN {dn.Code}.");

            var available = dnLine.DispatchedQuantity - dnLine.ReturnedQuantity;
            if (input.ReturnedQuantity > available)
            {
                return ApiResponse<CustomerReturnNoteDto>.Fail(
                    $"{dnLine.SalesOrderLine.Product.Name}: return qty {input.ReturnedQuantity:0.####} " +
                    $"exceeds available {available:0.####} (dispatched {dnLine.DispatchedQuantity:0.####}, " +
                    $"already returned {dnLine.ReturnedQuantity:0.####}).");
            }
        }

        var code = await _numbering.NextAsync("CRN", null, cancellationToken);

        var entity = new Domain.Entities.CustomerReturnNote
        {
            Code = code,
            DeliveryNoteId = cmd.DeliveryNoteId,
            ReturnDate = cmd.ReturnDate,
            ReturnWarehouseId = cmd.ReturnWarehouseId,
            Status = Domain.Entities.CustomerReturnNoteStatus.Draft,
            VehicleNumber = cmd.VehicleNumber,
            Reason = cmd.Reason,
            Notes = cmd.Notes,
            Lines = cmd.Lines.Select((l, i) =>
            {
                var dnLine = dnLineById[l.DeliveryNoteLineId];
                return new Domain.Entities.CustomerReturnNoteLine
                {
                    DeliveryNoteLineId = l.DeliveryNoteLineId,
                    ProductId = dnLine.SalesOrderLine.ProductId,
                    ReturnedQuantity = l.ReturnedQuantity,
                    SortOrder = i,
                    LineNotes = l.LineNotes
                };
            }).ToList()
        };

        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetCustomerReturnNoteByIdQuery(entity.Id), cancellationToken);
    }
}
