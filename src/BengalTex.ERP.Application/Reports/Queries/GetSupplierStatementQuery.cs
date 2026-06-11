using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Supplier Statement of Account (AP mirror of the customer statement) — opening payable,
/// chronological in-window movements (supplier invoices as credits: payable up; payments
/// as debits: payable down), running balance, closing payable. All amounts in base BDT
/// (each line × its source invoice's ExchangeRate). Excludes Draft + Cancelled invoices.
/// v1 doesn't include Debit Notes — add when DN-AP settlement mechanics solidify.
/// </summary>
public sealed record GetSupplierStatementQuery(
    int SupplierId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<SupplierStatementReportDto>>;

internal sealed class GetSupplierStatementQueryHandler
    : IRequestHandler<GetSupplierStatementQuery, ApiResponse<SupplierStatementReportDto>>
{
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _invRepo;
    private readonly IRepository<Domain.Entities.Payment, long> _paymentRepo;
    private readonly IRepository<Domain.Entities.Supplier> _supplierRepo;

    public GetSupplierStatementQueryHandler(
        IRepository<Domain.Entities.SupplierInvoice, long> invRepo,
        IRepository<Domain.Entities.Payment, long> paymentRepo,
        IRepository<Domain.Entities.Supplier> supplierRepo)
    {
        _invRepo = invRepo; _paymentRepo = paymentRepo; _supplierRepo = supplierRepo;
    }

    public async Task<ApiResponse<SupplierStatementReportDto>> Handle(
        GetSupplierStatementQuery req, CancellationToken ct)
    {
        var supplier = await _supplierRepo.GetByIdAsync(req.SupplierId, ct);
        if (supplier is null)
            return ApiResponse<SupplierStatementReportDto>.Fail("Supplier not found.");

        var toDate = req.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var fromDate = req.FromDate ?? toDate.AddMonths(-3);

        // Opening payable: (non-Draft, non-Cancelled) invoice credits dated < fromDate
        // minus payment debits dated < fromDate, in base BDT.
        var openInvoices = await _invRepo.Query()
            .Where(i => i.SupplierId == req.SupplierId
                     && i.Status != SupplierInvoiceStatus.Draft
                     && i.Status != SupplierInvoiceStatus.Cancelled
                     && i.InvoiceDate < fromDate)
            .Select(i => i.TotalAmount * i.ExchangeRate)
            .ToListAsync(ct);
        var openPayments = await _paymentRepo.Query()
            .Where(p => p.SupplierInvoice.SupplierId == req.SupplierId
                     && p.PaymentDate < fromDate)
            .Select(p => p.Amount * p.SupplierInvoice.ExchangeRate)
            .ToListAsync(ct);
        var opening = openInvoices.Sum() - openPayments.Sum();

        // In-window lines.
        var invLines = await _invRepo.Query()
            .Where(i => i.SupplierId == req.SupplierId
                     && i.Status != SupplierInvoiceStatus.Draft
                     && i.Status != SupplierInvoiceStatus.Cancelled
                     && i.InvoiceDate >= fromDate
                     && i.InvoiceDate <= toDate)
            .Select(i => new
            {
                Date = i.InvoiceDate,
                Code = i.Code,
                PurchaseOrderCode = i.PurchaseOrder.Code,
                SupplierInvoiceNumber = i.SupplierInvoiceNumber,
                AmountBase = i.TotalAmount * i.ExchangeRate
            })
            .ToListAsync(ct);

        var paymentLines = await _paymentRepo.Query()
            .Where(p => p.SupplierInvoice.SupplierId == req.SupplierId
                     && p.PaymentDate >= fromDate
                     && p.PaymentDate <= toDate)
            .Select(p => new
            {
                Date = p.PaymentDate,
                Code = p.Code,
                Method = p.PaymentMethod,
                AmountBase = p.Amount * p.SupplierInvoice.ExchangeRate
            })
            .ToListAsync(ct);

        var unsorted = new List<(DateOnly Date, string Type, string Reference, string? DocRef, decimal Debit, decimal Credit, long Tiebreaker)>();
        foreach (var i in invLines)
        {
            var docRef = string.IsNullOrWhiteSpace(i.SupplierInvoiceNumber)
                ? i.PurchaseOrderCode
                : $"{i.PurchaseOrderCode} / {i.SupplierInvoiceNumber}";
            unsorted.Add((i.Date, "Invoice", i.Code, docRef, 0m, i.AmountBase, 0));
        }
        foreach (var p in paymentLines)
            unsorted.Add((p.Date, "Payment", p.Code, p.Method.ToString(), p.AmountBase, 0m, 1)); // payments after invoices on same day

        var ordered = unsorted
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Tiebreaker)
            .ThenBy(x => x.Reference, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var running = opening;
        var lineDtos = new List<SupplierStatementLineDto>(ordered.Count);
        foreach (var x in ordered)
        {
            running += x.Credit - x.Debit;     // AP: credit raises payable, debit lowers it
            lineDtos.Add(new SupplierStatementLineDto(
                x.Date, x.Type, x.Reference, x.DocRef, x.Debit, x.Credit, running));
        }

        var totalCredits = lineDtos.Sum(l => l.Credit);
        var totalDebits = lineDtos.Sum(l => l.Debit);
        var closing = opening + totalCredits - totalDebits;

        var report = new SupplierStatementReportDto(
            fromDate, toDate,
            supplier.Id, supplier.Code, supplier.Name, supplier.Email,
            opening, totalCredits, totalDebits, closing,
            lineDtos.Count, lineDtos);

        return ApiResponse<SupplierStatementReportDto>.Ok(report);
    }
}
