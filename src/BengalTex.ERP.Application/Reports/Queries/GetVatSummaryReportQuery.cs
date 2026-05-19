using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Output VAT (collected from customers via Issued/PartiallyPaid/Paid invoices) vs
/// Input VAT (paid to suppliers via Approved/PartiallyPaid/Paid invoices) in a
/// date window, plus a monthly breakdown. Net Liability = Output − Input (positive
/// means we owe NBR for the period).
/// Default window: current calendar month.
/// </summary>
public sealed record GetVatSummaryReportQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<VatSummaryReportDto>>;

internal sealed class GetVatSummaryReportQueryHandler
    : IRequestHandler<GetVatSummaryReportQuery, ApiResponse<VatSummaryReportDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _arRepo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _apRepo;

    public GetVatSummaryReportQueryHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> arRepo,
        IRepository<Domain.Entities.SupplierInvoice, long> apRepo)
    {
        _arRepo = arRepo;
        _apRepo = apRepo;
    }

    public async Task<ApiResponse<VatSummaryReportDto>> Handle(
        GetVatSummaryReportQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var from = request.FromDate ?? new DateOnly(today.Year, today.Month, 1);
        var to = request.ToDate ?? from.AddMonths(1).AddDays(-1);

        // Output VAT — from customer invoices that have been Issued or further
        var arRows = await _arRepo.Query()
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to
                     && i.Status != Domain.Entities.CustomerInvoiceStatus.Draft
                     && i.Status != Domain.Entities.CustomerInvoiceStatus.Cancelled)
            .Select(i => new
            {
                i.InvoiceDate,
                i.SubtotalAmount,
                i.VatAmount,
                i.TotalAmount
            })
            .ToListAsync(cancellationToken);

        // Input VAT — from supplier invoices that have been Approved or further
        var apRows = await _apRepo.Query()
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to
                     && i.Status != Domain.Entities.SupplierInvoiceStatus.Draft
                     && i.Status != Domain.Entities.SupplierInvoiceStatus.Cancelled)
            .Select(i => new
            {
                i.InvoiceDate,
                i.SubtotalAmount,
                i.VatAmount,
                i.TotalAmount
            })
            .ToListAsync(cancellationToken);

        // Group by year+month for the monthly breakdown
        var arByMonth = arRows
            .GroupBy(r => new { r.InvoiceDate.Year, r.InvoiceDate.Month })
            .ToDictionary(g => (g.Key.Year, g.Key.Month), g => new
            {
                Net = g.Sum(x => x.SubtotalAmount),
                Vat = g.Sum(x => x.VatAmount)
            });

        var apByMonth = apRows
            .GroupBy(r => new { r.InvoiceDate.Year, r.InvoiceDate.Month })
            .ToDictionary(g => (g.Key.Year, g.Key.Month), g => new
            {
                Net = g.Sum(x => x.SubtotalAmount),
                Vat = g.Sum(x => x.VatAmount)
            });

        // Enumerate every month in the [from, to] window
        var months = new List<VatSummaryMonthDto>();
        var cursor = new DateOnly(from.Year, from.Month, 1);
        var lastMonth = new DateOnly(to.Year, to.Month, 1);
        while (cursor <= lastMonth)
        {
            var key = (cursor.Year, cursor.Month);
            var ar = arByMonth.TryGetValue(key, out var a) ? a : new { Net = 0m, Vat = 0m };
            var ap = apByMonth.TryGetValue(key, out var p) ? p : new { Net = 0m, Vat = 0m };

            var label = CultureInfo.InvariantCulture.DateTimeFormat
                .GetMonthName(cursor.Month) + " " + cursor.Year;

            months.Add(new VatSummaryMonthDto(
                cursor.Year, cursor.Month, label,
                ar.Net, ar.Vat,
                ap.Net, ap.Vat,
                ar.Vat - ap.Vat));
            cursor = cursor.AddMonths(1);
        }

        var outputNet   = arRows.Sum(x => x.SubtotalAmount);
        var outputVat   = arRows.Sum(x => x.VatAmount);
        var outputGross = arRows.Sum(x => x.TotalAmount);
        var inputNet    = apRows.Sum(x => x.SubtotalAmount);
        var inputVat    = apRows.Sum(x => x.VatAmount);
        var inputGross  = apRows.Sum(x => x.TotalAmount);

        var report = new VatSummaryReportDto(
            FromDate: from,
            ToDate: to,
            CustomerInvoiceCount: arRows.Count,
            OutputVatNet: outputNet,
            OutputVatAmount: outputVat,
            OutputVatGross: outputGross,
            SupplierInvoiceCount: apRows.Count,
            InputVatNet: inputNet,
            InputVatAmount: inputVat,
            InputVatGross: inputGross,
            NetVatLiability: outputVat - inputVat,
            Months: months);

        return ApiResponse<VatSummaryReportDto>.Ok(report);
    }
}
