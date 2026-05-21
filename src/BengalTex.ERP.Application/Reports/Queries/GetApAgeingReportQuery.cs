using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Supplier outstanding broken into past-due buckets as of a given date. Mirror of
/// <see cref="GetArAgeingReportQuery"/>. Includes only invoices with Status ∈
/// {Approved, PartiallyPaid} AND AmountDue &gt; 0.
/// </summary>
public sealed record GetApAgeingReportQuery(
    DateOnly? AsOfDate = null,
    int? SupplierId = null
) : IRequest<ApiResponse<ApAgeingReportDto>>;

internal sealed class GetApAgeingReportQueryHandler
    : IRequestHandler<GetApAgeingReportQuery, ApiResponse<ApAgeingReportDto>>
{
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _invRepo;

    public GetApAgeingReportQueryHandler(IRepository<Domain.Entities.SupplierInvoice, long> invRepo)
        => _invRepo = invRepo;

    public async Task<ApiResponse<ApAgeingReportDto>> Handle(
        GetApAgeingReportQuery request, CancellationToken cancellationToken)
    {
        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.Today);

        var query = _invRepo.Query()
            .Where(i => (i.Status == Domain.Entities.SupplierInvoiceStatus.Approved
                      || i.Status == Domain.Entities.SupplierInvoiceStatus.PartiallyPaid)
                     && i.TotalAmount - i.AmountPaid > 0m);

        if (request.SupplierId.HasValue)
            query = query.Where(i => i.SupplierId == request.SupplierId.Value);

        var raw = await query
            .Include(i => i.Supplier)
            .Include(i => i.PurchaseOrder)
            .AsNoTracking()
            .Select(i => new
            {
                i.Id,
                i.Code,
                PurchaseOrderCode = i.PurchaseOrder.Code,
                i.SupplierInvoiceNumber,
                i.SupplierId,
                SupplierCode = i.Supplier.Code,
                SupplierName = i.Supplier.Name,
                i.InvoiceDate,
                i.DueDate,
                // Convert to base currency (BDT) so a mixed-currency ageing report sums correctly.
                TotalAmount = i.TotalAmount * i.ExchangeRate,
                AmountPaid = i.AmountPaid * i.ExchangeRate
            })
            .ToListAsync(cancellationToken);

        var invoiceDtos = raw
            .Select(r =>
            {
                var daysPastDue = asOf.DayNumber - r.DueDate.DayNumber;
                var bucket = BucketOf(daysPastDue);
                return new
                {
                    r.SupplierId,
                    r.SupplierCode,
                    r.SupplierName,
                    Invoice = new ApAgeingInvoiceDto(
                        r.Id,
                        r.Code,
                        r.PurchaseOrderCode,
                        r.SupplierInvoiceNumber,
                        r.InvoiceDate,
                        r.DueDate,
                        daysPastDue,
                        bucket,
                        r.TotalAmount,
                        r.AmountPaid,
                        r.TotalAmount - r.AmountPaid)
                };
            })
            .ToList();

        var supplierGroups = invoiceDtos
            .GroupBy(x => new { x.SupplierId, x.SupplierCode, x.SupplierName })
            .Select(g =>
            {
                var current    = g.Where(x => x.Invoice.Bucket == "Current").Sum(x => x.Invoice.AmountDue);
                var days1to30  = g.Where(x => x.Invoice.Bucket == "1-30").Sum(x => x.Invoice.AmountDue);
                var days31to60 = g.Where(x => x.Invoice.Bucket == "31-60").Sum(x => x.Invoice.AmountDue);
                var days61to90 = g.Where(x => x.Invoice.Bucket == "61-90").Sum(x => x.Invoice.AmountDue);
                var days90plus = g.Where(x => x.Invoice.Bucket == "90+").Sum(x => x.Invoice.AmountDue);
                var total = current + days1to30 + days31to60 + days61to90 + days90plus;

                return new ApAgeingSupplierDto(
                    g.Key.SupplierId,
                    g.Key.SupplierCode,
                    g.Key.SupplierName,
                    current, days1to30, days31to60, days61to90, days90plus,
                    total,
                    g.Count(),
                    g.OrderBy(x => x.Invoice.DueDate)
                     .Select(x => x.Invoice)
                     .ToList());
            })
            .OrderByDescending(c => c.TotalOutstanding)
            .ToList();

        var report = new ApAgeingReportDto(
            AsOfDate: asOf,
            SupplierCount: supplierGroups.Count,
            InvoiceCount: invoiceDtos.Count,
            TotalCurrent:    supplierGroups.Sum(s => s.Current),
            Total1To30:      supplierGroups.Sum(s => s.Days1To30),
            Total31To60:     supplierGroups.Sum(s => s.Days31To60),
            Total61To90:     supplierGroups.Sum(s => s.Days61To90),
            Total90Plus:     supplierGroups.Sum(s => s.Days90Plus),
            TotalOutstanding: supplierGroups.Sum(s => s.TotalOutstanding),
            Suppliers: supplierGroups);

        return ApiResponse<ApAgeingReportDto>.Ok(report);
    }

    private static string BucketOf(int daysPastDue) => daysPastDue switch
    {
        <= 0 => "Current",
        <= 30 => "1-30",
        <= 60 => "31-60",
        <= 90 => "61-90",
        _ => "90+"
    };
}
