using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Customer Statement of Account — opening balance, chronological in-window movements
/// (invoices as debits, receipts as credits), running balance, closing balance.
/// All amounts in base BDT (each line × its source ExchangeRate). Excludes Draft +
/// Cancelled invoices. v1 doesn't yet include Credit/Debit Notes — add when CN/DN
/// settlement mechanics solidify in this module.
/// </summary>
public sealed record GetCustomerStatementQuery(
    int CustomerId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<CustomerStatementReportDto>>;

internal sealed class GetCustomerStatementQueryHandler
    : IRequestHandler<GetCustomerStatementQuery, ApiResponse<CustomerStatementReportDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly IRepository<Domain.Entities.Receipt, long> _receiptRepo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;

    public GetCustomerStatementQueryHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IRepository<Domain.Entities.Receipt, long> receiptRepo,
        IRepository<Domain.Entities.Customer> customerRepo)
    {
        _invRepo = invRepo; _receiptRepo = receiptRepo; _customerRepo = customerRepo;
    }

    public async Task<ApiResponse<CustomerStatementReportDto>> Handle(
        GetCustomerStatementQuery req, CancellationToken ct)
    {
        var customer = await _customerRepo.GetByIdAsync(req.CustomerId, ct);
        if (customer is null)
            return ApiResponse<CustomerStatementReportDto>.Fail("Customer not found.");

        var toDate = req.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var fromDate = req.FromDate ?? toDate.AddMonths(-3);

        // Opening balance: all (non-Draft, non-Cancelled) invoice debits dated < fromDate
        // minus all receipt credits dated < fromDate, in base BDT.
        var openInvoices = await _invRepo.Query()
            .Where(i => i.CustomerId == req.CustomerId
                     && i.Status != CustomerInvoiceStatus.Draft
                     && i.Status != CustomerInvoiceStatus.Cancelled
                     && i.InvoiceDate < fromDate)
            .Select(i => i.TotalAmount * i.ExchangeRate)
            .ToListAsync(ct);
        var openReceipts = await _receiptRepo.Query()
            .Where(r => r.CustomerInvoice.CustomerId == req.CustomerId
                     && r.ReceiptDate < fromDate)
            .Select(r => r.Amount * r.CustomerInvoice.ExchangeRate)
            .ToListAsync(ct);
        var opening = openInvoices.Sum() - openReceipts.Sum();

        // In-window lines.
        var invLines = await _invRepo.Query()
            .Where(i => i.CustomerId == req.CustomerId
                     && i.Status != CustomerInvoiceStatus.Draft
                     && i.Status != CustomerInvoiceStatus.Cancelled
                     && i.InvoiceDate >= fromDate
                     && i.InvoiceDate <= toDate)
            .Select(i => new
            {
                Date = i.InvoiceDate,
                Code = i.Code,
                SalesOrderCode = i.SalesOrder.Code,
                AmountBase = i.TotalAmount * i.ExchangeRate
            })
            .ToListAsync(ct);

        var receiptLines = await _receiptRepo.Query()
            .Where(r => r.CustomerInvoice.CustomerId == req.CustomerId
                     && r.ReceiptDate >= fromDate
                     && r.ReceiptDate <= toDate)
            .Select(r => new
            {
                Date = r.ReceiptDate,
                Code = r.Code,
                InvoiceCode = r.CustomerInvoice.Code,
                Method = r.PaymentMethod,
                AmountBase = r.Amount * r.CustomerInvoice.ExchangeRate
            })
            .ToListAsync(ct);

        var unsorted = new List<(DateOnly Date, string Type, string Reference, string? DocRef, decimal Debit, decimal Credit, long Tiebreaker)>();
        foreach (var i in invLines)
            unsorted.Add((i.Date, "Invoice", i.Code, i.SalesOrderCode, i.AmountBase, 0m, 0));
        foreach (var r in receiptLines)
            unsorted.Add((r.Date, "Receipt", r.Code, r.Method.ToString(), 0m, r.AmountBase, 1)); // receipts after invoices on same day

        var ordered = unsorted
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Tiebreaker)
            .ThenBy(x => x.Reference, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var running = opening;
        var lineDtos = new List<CustomerStatementLineDto>(ordered.Count);
        foreach (var x in ordered)
        {
            running += x.Debit - x.Credit;
            lineDtos.Add(new CustomerStatementLineDto(
                x.Date, x.Type, x.Reference, x.DocRef, x.Debit, x.Credit, running));
        }

        var totalDebits = lineDtos.Sum(l => l.Debit);
        var totalCredits = lineDtos.Sum(l => l.Credit);
        var closing = opening + totalDebits - totalCredits;

        var report = new CustomerStatementReportDto(
            fromDate, toDate,
            customer.Id, customer.Code, customer.Name, customer.Email,
            opening, totalDebits, totalCredits, closing,
            lineDtos.Count, lineDtos);

        return ApiResponse<CustomerStatementReportDto>.Ok(report);
    }
}
