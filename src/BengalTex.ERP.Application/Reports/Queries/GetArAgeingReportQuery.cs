using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Customer outstanding broken into past-due buckets as of a given date. Includes only
/// invoices with Status ∈ {Issued, PartiallyPaid} AND AmountDue > 0. Bucketization is
/// done in-memory after a single round-trip — outstanding-invoice volume is naturally
/// small (typically &lt; 1000 rows for an SME factory).
/// </summary>
public sealed record GetArAgeingReportQuery(
    DateOnly? AsOfDate = null,
    int? CustomerId = null
) : IRequest<ApiResponse<ArAgeingReportDto>>;

internal sealed class GetArAgeingReportQueryHandler
    : IRequestHandler<GetArAgeingReportQuery, ApiResponse<ArAgeingReportDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;

    public GetArAgeingReportQueryHandler(IRepository<Domain.Entities.CustomerInvoice, long> invRepo)
        => _invRepo = invRepo;

    public async Task<ApiResponse<ArAgeingReportDto>> Handle(
        GetArAgeingReportQuery request, CancellationToken cancellationToken)
    {
        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.Today);

        var query = _invRepo.Query()
            .Where(i => (i.Status == Domain.Entities.CustomerInvoiceStatus.Issued
                      || i.Status == Domain.Entities.CustomerInvoiceStatus.PartiallyPaid)
                     && i.TotalAmount - i.AmountPaid > 0m);

        if (request.CustomerId.HasValue)
            query = query.Where(i => i.CustomerId == request.CustomerId.Value);

        var raw = await query
            .Include(i => i.Customer)
            .Include(i => i.SalesOrder)
            .AsNoTracking()
            .Select(i => new
            {
                i.Id,
                i.Code,
                SalesOrderCode = i.SalesOrder.Code,
                i.CustomerId,
                CustomerCode = i.Customer.Code,
                CustomerName = i.Customer.Name,
                i.InvoiceDate,
                i.DueDate,
                i.TotalAmount,
                i.AmountPaid
            })
            .ToListAsync(cancellationToken);

        var invoiceDtos = raw
            .Select(r =>
            {
                var daysPastDue = asOf.DayNumber - r.DueDate.DayNumber;
                var bucket = BucketOf(daysPastDue);
                return new
                {
                    r.CustomerId,
                    r.CustomerCode,
                    r.CustomerName,
                    Invoice = new ArAgeingInvoiceDto(
                        r.Id,
                        r.Code,
                        r.SalesOrderCode,
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

        var customerGroups = invoiceDtos
            .GroupBy(x => new { x.CustomerId, x.CustomerCode, x.CustomerName })
            .Select(g =>
            {
                var current   = g.Where(x => x.Invoice.Bucket == "Current").Sum(x => x.Invoice.AmountDue);
                var days1to30 = g.Where(x => x.Invoice.Bucket == "1-30").Sum(x => x.Invoice.AmountDue);
                var days31to60= g.Where(x => x.Invoice.Bucket == "31-60").Sum(x => x.Invoice.AmountDue);
                var days61to90= g.Where(x => x.Invoice.Bucket == "61-90").Sum(x => x.Invoice.AmountDue);
                var days90plus= g.Where(x => x.Invoice.Bucket == "90+").Sum(x => x.Invoice.AmountDue);
                var total = current + days1to30 + days31to60 + days61to90 + days90plus;

                return new ArAgeingCustomerDto(
                    g.Key.CustomerId,
                    g.Key.CustomerCode,
                    g.Key.CustomerName,
                    current, days1to30, days31to60, days61to90, days90plus,
                    total,
                    g.Count(),
                    g.OrderBy(x => x.Invoice.DueDate)
                     .Select(x => x.Invoice)
                     .ToList());
            })
            .OrderByDescending(c => c.TotalOutstanding)
            .ToList();

        var report = new ArAgeingReportDto(
            AsOfDate: asOf,
            CustomerCount: customerGroups.Count,
            InvoiceCount: invoiceDtos.Count,
            TotalCurrent:    customerGroups.Sum(c => c.Current),
            Total1To30:      customerGroups.Sum(c => c.Days1To30),
            Total31To60:     customerGroups.Sum(c => c.Days31To60),
            Total61To90:     customerGroups.Sum(c => c.Days61To90),
            Total90Plus:     customerGroups.Sum(c => c.Days90Plus),
            TotalOutstanding: customerGroups.Sum(c => c.TotalOutstanding),
            Customers: customerGroups);

        return ApiResponse<ArAgeingReportDto>.Ok(report);
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
