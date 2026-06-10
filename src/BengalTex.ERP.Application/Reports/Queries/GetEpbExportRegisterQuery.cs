using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// EPB Export Register — lists every foreign-currency (non-BDT) CustomerInvoice issued in
/// the date range, in the shape Bangladesh's Export Promotion Bureau Form-N needs. Foreign
/// currency is detected by `Currency.IsBaseCurrency = false` (so any non-BDT counts as
/// "export" for v1; refine to a Customer.IsExport flag in v1b if required). Default window
/// = trailing 30 days. Drives both the on-screen report and CSV export.
/// </summary>
public sealed record GetEpbExportRegisterQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    bool PendingFormExpOnly = false
) : IRequest<ApiResponse<EpbExportRegisterReportDto>>;

internal sealed class GetEpbExportRegisterQueryHandler
    : IRequestHandler<GetEpbExportRegisterQuery, ApiResponse<EpbExportRegisterReportDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    public GetEpbExportRegisterQueryHandler(IRepository<Domain.Entities.CustomerInvoice, long> invRepo)
        => _invRepo = invRepo;

    public async Task<ApiResponse<EpbExportRegisterReportDto>> Handle(
        GetEpbExportRegisterQuery req, CancellationToken ct)
    {
        var toDate = req.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var fromDate = req.FromDate ?? toDate.AddDays(-30);

        var query = _invRepo.Query()
            .Where(i => i.Status != CustomerInvoiceStatus.Draft
                     && i.Status != CustomerInvoiceStatus.Cancelled
                     && !i.Currency.IsBaseCurrency               // foreign currency → export
                     && i.InvoiceDate >= fromDate
                     && i.InvoiceDate <= toDate);

        if (req.PendingFormExpOnly)
            query = query.Where(i => i.EpbFormNumber == null);

        var rows = await query
            .OrderByDescending(i => i.ShipmentDate ?? i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Select(i => new EpbExportRegisterRowDto(
                i.Id, i.Code,
                i.InvoiceDate, i.ShipmentDate, i.EpbFormNumber, i.LcNumber,
                i.CustomerId, i.Customer.Code, i.Customer.Name,
                i.Customer.Country,
                i.SalesOrder.Code,
                i.Currency.Code, i.ExchangeRate,
                i.SubtotalAmount, i.SubtotalAmount * i.ExchangeRate,
                i.TotalAmount, i.TotalAmount * i.ExchangeRate,
                i.Status.ToString(),
                // Distinct HS codes from lines, joined; null when no HS codes set yet
                string.Join(", ",
                    i.Lines.Where(l => l.Product.HsCode != null && l.Product.HsCode != "")
                           .Select(l => l.Product.HsCode!)
                           .Distinct())))
            .ToListAsync(ct);

        // EF Core translates `string.Join` w/ empty Distinct to "" — normalise to null for UI
        var cleaned = rows.Select(r =>
            string.IsNullOrEmpty(r.HsCodesSummary)
                ? r with { HsCodesSummary = null }
                : r).ToList();

        var totalInvoices = cleaned.Count;
        var pendingForm = cleaned.Count(r => string.IsNullOrEmpty(r.EpbFormNumber));
        var grandFobBdt = cleaned.Sum(r => r.FobAmountBdt);
        var grandTotalBdt = cleaned.Sum(r => r.TotalAmountBdt);

        return ApiResponse<EpbExportRegisterReportDto>.Ok(new EpbExportRegisterReportDto(
            fromDate, toDate,
            totalInvoices, pendingForm,
            grandFobBdt, grandTotalBdt,
            cleaned));
    }
}
