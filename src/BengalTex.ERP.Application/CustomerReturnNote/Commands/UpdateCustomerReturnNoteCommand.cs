using BengalTex.ERP.Application.CustomerReturnNote.Dtos;
using BengalTex.ERP.Application.CustomerReturnNote.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerReturnNote.Commands;

/// <summary>
/// Updates a Draft Customer Return Note. Lines are fully replaced (clear-and-recreate)
/// like other transactional documents. Posted CRNs are immutable.
/// </summary>
public sealed record UpdateCustomerReturnNoteCommand(
    long Id,
    int ReturnWarehouseId,
    DateOnly ReturnDate,
    string? VehicleNumber,
    string? Reason,
    string? Notes,
    IReadOnlyList<CustomerReturnNoteLineInput> Lines
) : IRequest<ApiResponse<CustomerReturnNoteDto>>;

public sealed class UpdateCustomerReturnNoteCommandValidator : AbstractValidator<UpdateCustomerReturnNoteCommand>
{
    public UpdateCustomerReturnNoteCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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

internal sealed class UpdateCustomerReturnNoteCommandHandler
    : IRequestHandler<UpdateCustomerReturnNoteCommand, ApiResponse<CustomerReturnNoteDto>>
{
    private readonly IRepository<Domain.Entities.CustomerReturnNote, long> _repo;
    private readonly IRepository<Domain.Entities.DeliveryNote, long> _dnRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateCustomerReturnNoteCommandHandler(
        IRepository<Domain.Entities.CustomerReturnNote, long> repo,
        IRepository<Domain.Entities.DeliveryNote, long> dnRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _dnRepo = dnRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerReturnNoteDto>> Handle(
        UpdateCustomerReturnNoteCommand cmd, CancellationToken cancellationToken)
    {
        var crn = await _repo.Query()
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken);

        if (crn is null) return ApiResponse<CustomerReturnNoteDto>.Fail("Customer return note not found.");
        if (crn.Status != Domain.Entities.CustomerReturnNoteStatus.Draft)
            return ApiResponse<CustomerReturnNoteDto>.Fail("Only draft customer return notes can be edited.");

        var dn = await _dnRepo.Query()
            .Include(d => d.Lines).ThenInclude(l => l.SalesOrderLine).ThenInclude(sl => sl.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == crn.DeliveryNoteId, cancellationToken);
        if (dn is null) return ApiResponse<CustomerReturnNoteDto>.Fail("Parent delivery note not found.");

        var dnLineById = dn.Lines.ToDictionary(l => l.Id);
        foreach (var input in cmd.Lines)
        {
            if (!dnLineById.TryGetValue(input.DeliveryNoteLineId, out var dnLine))
                return ApiResponse<CustomerReturnNoteDto>.Fail(
                    $"Delivery-note line {input.DeliveryNoteLineId} does not belong to DN {dn.Code}.");

            // Available = dispatched − previously-returned (excluding any rows from THIS Draft CRN
            // which are about to be replaced anyway)
            var available = dnLine.DispatchedQuantity - dnLine.ReturnedQuantity;
            if (input.ReturnedQuantity > available)
            {
                return ApiResponse<CustomerReturnNoteDto>.Fail(
                    $"{dnLine.SalesOrderLine.Product.Name}: return qty {input.ReturnedQuantity:0.####} " +
                    $"exceeds available {available:0.####} (dispatched {dnLine.DispatchedQuantity:0.####}, " +
                    $"already returned {dnLine.ReturnedQuantity:0.####}).");
            }
        }

        crn.ReturnWarehouseId = cmd.ReturnWarehouseId;
        crn.ReturnDate = cmd.ReturnDate;
        crn.VehicleNumber = cmd.VehicleNumber;
        crn.Reason = cmd.Reason;
        crn.Notes = cmd.Notes;

        crn.Lines.Clear();
        var sortOrder = 0;
        foreach (var line in cmd.Lines)
        {
            var dnLine = dnLineById[line.DeliveryNoteLineId];
            crn.Lines.Add(new Domain.Entities.CustomerReturnNoteLine
            {
                DeliveryNoteLineId = line.DeliveryNoteLineId,
                ProductId = dnLine.SalesOrderLine.ProductId,
                ReturnedQuantity = line.ReturnedQuantity,
                SortOrder = sortOrder++,
                LineNotes = line.LineNotes
            });
        }

        _repo.Update(crn);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetCustomerReturnNoteByIdQuery(crn.Id), cancellationToken);
    }
}
