using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

/// <summary>One delivery-note line and how much of its remaining quantity to invoice this time.</summary>
public sealed record DeliveryInvoiceLineInput(long DeliveryNoteLineId, decimal Quantity);

/// <summary>
/// Generates a DRAFT customer invoice from a posted Delivery Note — the "delivery → invoice"
/// cross-module automation, now with PARTIAL-invoice tracking. Each DN line carries an
/// <see cref="Domain.Entities.DeliveryNoteLine.InvoicedQuantity"/> running total; only the
/// remaining (Dispatched − Invoiced) can be billed. Multiple partial invoices may be raised
/// against one DN until every line is fully invoiced — at which point no further invoice is
/// allowed (duplicate-invoice block).
///
/// <para><see cref="Lines"/> is optional: when null/empty the command invoices ALL remaining
/// quantity (backward-compatible one-click behaviour). When supplied, each entry's quantity is
/// validated to be &gt; 0 and ≤ that line's remaining.</para>
///
/// Lines are grouped per product (quantity-weighted price) at the originating Sales Order line
/// prices; currency/rate/due-date are inherited from the SO via
/// <see cref="CreateCustomerInvoiceCommand"/>. The result is a Draft the user reviews + issues.
/// </summary>
public sealed record CreateInvoiceFromDeliveryNoteCommand(
    long DeliveryNoteId,
    decimal VatRate = 0m,
    IReadOnlyList<DeliveryInvoiceLineInput>? Lines = null)
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
        // Tracked load — we increment InvoicedQuantity on the DN lines and let the inner
        // CreateCustomerInvoiceCommand's SaveChanges commit both atomically (shared DbContext).
        var dn = await _dnRepo.Query()
            .Include(d => d.Lines).ThenInclude(l => l.SalesOrderLine)
            .FirstOrDefaultAsync(d => d.Id == cmd.DeliveryNoteId, ct);

        if (dn is null) return ApiResponse<CustomerInvoiceDto>.Fail("Delivery note not found.");
        if (dn.Status != Domain.Entities.DeliveryNoteStatus.Posted)
            return ApiResponse<CustomerInvoiceDto>.Fail("Only posted delivery notes can be invoiced.");
        if (dn.Lines.Count == 0)
            return ApiResponse<CustomerInvoiceDto>.Fail("This delivery note has no lines.");

        // Decide how much to invoice on each DN line.
        // No explicit lines → invoice every line's remaining (one-click "invoice the rest").
        var requested = cmd.Lines is { Count: > 0 }
            ? cmd.Lines.Where(l => l.Quantity != 0m).ToDictionary(l => l.DeliveryNoteLineId, l => l.Quantity)
            : dn.Lines.ToDictionary(l => l.Id, l => l.DispatchedQuantity - l.InvoicedQuantity);

        // Validate each requested line against its remaining quantity.
        var toInvoice = new List<(Domain.Entities.DeliveryNoteLine Line, decimal Qty)>();
        foreach (var (lineId, qty) in requested)
        {
            var line = dn.Lines.FirstOrDefault(l => l.Id == lineId);
            if (line is null)
                return ApiResponse<CustomerInvoiceDto>.Fail($"Delivery-note line {lineId} does not belong to this delivery note.");

            var remaining = line.DispatchedQuantity - line.InvoicedQuantity;
            if (qty < 0m)
                return ApiResponse<CustomerInvoiceDto>.Fail("Invoice quantity cannot be negative.");
            if (qty > remaining)
                return ApiResponse<CustomerInvoiceDto>.Fail(
                    $"Cannot invoice {qty:0.####} — only {remaining:0.####} remains uninvoiced on this line.");

            if (qty > 0m) toInvoice.Add((line, qty));
        }

        if (toInvoice.Count == 0)
            return ApiResponse<CustomerInvoiceDto>.Fail(
                "Nothing left to invoice — this delivery note is already fully invoiced.");

        // One invoice line per product (a DN may dispatch the same product across several SO lines).
        // Quantity-weighted price keeps the value exact even if SO-line prices differ.
        var lines = toInvoice
            .GroupBy(t => t.Line.SalesOrderLine.ProductId)
            .Select(g =>
            {
                var qty = g.Sum(x => x.Qty);
                var price = qty > 0m ? g.Sum(x => x.Qty * x.Line.SalesOrderLine.UnitPrice) / qty : 0m;
                return new CustomerInvoiceLineInput(g.Key, qty, price, null);
            })
            .ToList();

        // Increment InvoicedQuantity on the tracked DN lines BEFORE the inner command saves —
        // its SaveChanges then commits the invoice + these increments in one transaction.
        foreach (var (line, qty) in toInvoice)
            line.InvoicedQuantity += qty;

        // Reuse the standard invoice-creation path (numbering, currency inheritance, VAT, totals).
        return await _mediator.Send(new CreateCustomerInvoiceCommand(
            dn.SalesOrderId, cmd.VatRate,
            DateOnly.FromDateTime(DateTime.UtcNow), null,
            $"From Delivery Note {dn.Code}", lines), ct);
    }
}
