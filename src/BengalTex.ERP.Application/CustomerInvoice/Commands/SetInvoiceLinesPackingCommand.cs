using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Application.CustomerInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

/// <summary>One line's packing breakdown — all fields optional (null clears).</summary>
public sealed record InvoiceLinePackingInput(
    long LineId,
    int? CartonNumberFrom,
    int? CartonNumberTo,
    int? UnitsPerCarton,
    decimal? NetWeightKgPerLine,
    decimal? GrossWeightKgPerLine);

/// <summary>
/// Bulk-set per-line packing data on a non-Draft, non-Cancelled CustomerInvoice.
/// Editable at any post-Draft stage — packing data is metadata for the Packing List,
/// doesn't change qty/price/totals, so doesn't touch the AR lifecycle. Lines not in
/// the payload are left untouched (partial updates are valid).
/// </summary>
public sealed record SetInvoiceLinesPackingCommand(
    long InvoiceId,
    IReadOnlyList<InvoiceLinePackingInput> Lines
) : IRequest<ApiResponse<CustomerInvoiceDto>>;

public sealed class SetInvoiceLinesPackingCommandValidator : AbstractValidator<SetInvoiceLinesPackingCommand>
{
    public SetInvoiceLinesPackingCommandValidator()
    {
        RuleFor(x => x.InvoiceId).GreaterThan(0);
        RuleFor(x => x.Lines).NotNull();
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(p => p.LineId).GreaterThan(0);
            l.RuleFor(p => p.CartonNumberFrom).GreaterThanOrEqualTo(0).When(p => p.CartonNumberFrom.HasValue);
            l.RuleFor(p => p.CartonNumberTo).GreaterThanOrEqualTo(0).When(p => p.CartonNumberTo.HasValue);
            l.RuleFor(p => p.UnitsPerCarton).GreaterThanOrEqualTo(0).When(p => p.UnitsPerCarton.HasValue);
            l.RuleFor(p => p.NetWeightKgPerLine).GreaterThanOrEqualTo(0).When(p => p.NetWeightKgPerLine.HasValue);
            l.RuleFor(p => p.GrossWeightKgPerLine).GreaterThanOrEqualTo(0).When(p => p.GrossWeightKgPerLine.HasValue);
            l.RuleFor(p => p).Must(p => !p.CartonNumberFrom.HasValue || !p.CartonNumberTo.HasValue
                                         || p.CartonNumberTo.Value >= p.CartonNumberFrom.Value)
                .WithMessage("CartonNumberTo must be >= CartonNumberFrom.");
        });
    }
}

internal sealed class SetInvoiceLinesPackingCommandHandler
    : IRequestHandler<SetInvoiceLinesPackingCommand, ApiResponse<CustomerInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public SetInvoiceLinesPackingCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> repo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo; _uow = uow; _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerInvoiceDto>> Handle(
        SetInvoiceLinesPackingCommand cmd, CancellationToken ct)
    {
        var inv = await _repo.Query()
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == cmd.InvoiceId, ct);

        if (inv is null) return ApiResponse<CustomerInvoiceDto>.Fail("Invoice not found.");
        if (inv.Status == Domain.Entities.CustomerInvoiceStatus.Draft)
            return ApiResponse<CustomerInvoiceDto>.Fail("Issue the invoice first before recording packing details.");
        if (inv.Status == Domain.Entities.CustomerInvoiceStatus.Cancelled)
            return ApiResponse<CustomerInvoiceDto>.Fail("Cannot record packing on a cancelled invoice.");

        var byId = inv.Lines.ToDictionary(l => l.Id);
        foreach (var input in cmd.Lines)
        {
            if (!byId.TryGetValue(input.LineId, out var line))
                return ApiResponse<CustomerInvoiceDto>.Fail($"Line {input.LineId} does not belong to invoice {inv.Code}.");
            line.CartonNumberFrom = input.CartonNumberFrom;
            line.CartonNumberTo = input.CartonNumberTo;
            line.UnitsPerCarton = input.UnitsPerCarton;
            line.NetWeightKgPerLine = input.NetWeightKgPerLine;
            line.GrossWeightKgPerLine = input.GrossWeightKgPerLine;
        }

        _repo.Update(inv);
        await _uow.SaveChangesAsync(ct);

        return await _mediator.Send(new GetCustomerInvoiceByIdQuery(cmd.InvoiceId), ct);
    }
}
