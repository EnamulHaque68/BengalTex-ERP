using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

/// <summary>
/// Generates a DRAFT customer invoice from a posted Delivery Note — the "delivery → invoice"
/// cross-module automation. Lines are taken from the DN's dispatched quantities at the originating
/// Sales Order line prices (grouped per product, quantity-weighted price); currency/rate/due-date
/// are inherited from the SO via <see cref="CreateCustomerInvoiceCommand"/>. The result is a Draft
/// the user reviews + issues — nothing is posted automatically.
/// </summary>
public sealed record CreateInvoiceFromDeliveryNoteCommand(long DeliveryNoteId, decimal VatRate = 0m)
    : IRequest<ApiResponse<CustomerInvoiceDto>>;

internal sealed class CreateInvoiceFromDeliveryNoteCommandHandler
    : IRequestHandler<CreateInvoiceFromDeliveryNoteCommand, ApiResponse<CustomerInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.DeliveryNote, long> _dnRepo;
    private readonly IMediator _mediator;

    public CreateInvoiceFromDeliveryNoteCommandHandler(
        IRepository<Domain.Entities.DeliveryNote, long> dnRepo, IMediator mediator)
    {
        _dnRepo = dnRepo;
        _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerInvoiceDto>> Handle(
        CreateInvoiceFromDeliveryNoteCommand cmd, CancellationToken ct)
    {
        var dn = await _dnRepo.Query().AsNoTracking()
            .Include(d => d.Lines).ThenInclude(l => l.SalesOrderLine)
            .FirstOrDefaultAsync(d => d.Id == cmd.DeliveryNoteId, ct);

        if (dn is null) return ApiResponse<CustomerInvoiceDto>.Fail("Delivery note not found.");
        if (dn.Status != Domain.Entities.DeliveryNoteStatus.Posted)
            return ApiResponse<CustomerInvoiceDto>.Fail("Only posted delivery notes can be invoiced.");
        if (dn.Lines.Count == 0)
            return ApiResponse<CustomerInvoiceDto>.Fail("This delivery note has no lines.");

        // One invoice line per product (a DN may dispatch the same product across several SO lines).
        // Quantity-weighted price keeps the value exact even if SO-line prices differ.
        var lines = dn.Lines
            .GroupBy(l => l.SalesOrderLine.ProductId)
            .Select(g =>
            {
                var qty = g.Sum(x => x.DispatchedQuantity);
                var price = qty > 0m ? g.Sum(x => x.DispatchedQuantity * x.SalesOrderLine.UnitPrice) / qty : 0m;
                return new CustomerInvoiceLineInput(g.Key, qty, price, null);
            })
            .Where(l => l.Quantity > 0m)
            .ToList();

        if (lines.Count == 0)
            return ApiResponse<CustomerInvoiceDto>.Fail("Nothing dispatched on this delivery note to invoice.");

        // Reuse the standard invoice-creation path (numbering, currency inheritance, VAT, totals).
        return await _mediator.Send(new CreateCustomerInvoiceCommand(
            dn.SalesOrderId, cmd.VatRate,
            DateOnly.FromDateTime(DateTime.UtcNow), null,
            $"From Delivery Note {dn.Code}", lines), ct);
    }
}
