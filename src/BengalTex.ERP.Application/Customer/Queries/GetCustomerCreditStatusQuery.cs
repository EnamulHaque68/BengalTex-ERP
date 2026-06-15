using BengalTex.ERP.Application.Common.Settings;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Application.Customer.Queries;

/// <summary>
/// A customer's current credit standing in base BDT — used to show available credit on the
/// Sales Order form before an order is built (the SO Confirm check uses the same math).
/// </summary>
public sealed record CustomerCreditStatusDto(
    int CustomerId,
    decimal CreditLimit,          // 0 = no limit set (not enforced)
    decimal OutstandingAr,        // Σ Issued/PartiallyPaid (Total − Paid) × rate, BDT
    decimal AvailableCredit,      // CreditLimit − OutstandingAr (≥ 0; 0 when no limit)
    bool IsOverLimit,             // OutstandingAr already exceeds the limit
    bool Enforced);               // credit control is on AND set to block AND limit > 0

public sealed record GetCustomerCreditStatusQuery(int CustomerId)
    : IRequest<ApiResponse<CustomerCreditStatusDto>>;

internal sealed class GetCustomerCreditStatusQueryHandler
    : IRequestHandler<GetCustomerCreditStatusQuery, ApiResponse<CustomerCreditStatusDto>>
{
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly CreditControlSettings _credit;

    public GetCustomerCreditStatusQueryHandler(
        IRepository<Domain.Entities.Customer> customerRepo,
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IOptions<CreditControlSettings> credit)
    {
        _customerRepo = customerRepo;
        _invRepo = invRepo;
        _credit = credit.Value;
    }

    public async Task<ApiResponse<CustomerCreditStatusDto>> Handle(
        GetCustomerCreditStatusQuery req, CancellationToken ct)
    {
        var customer = await _customerRepo.GetByIdAsync(req.CustomerId, ct);
        if (customer is null) return ApiResponse<CustomerCreditStatusDto>.Fail("Customer not found.");

        var outstanding = await _invRepo.Query()
            .Where(i => i.CustomerId == req.CustomerId
                     && (i.Status == CustomerInvoiceStatus.Issued || i.Status == CustomerInvoiceStatus.PartiallyPaid)
                     && i.TotalAmount - i.AmountPaid > 0m)
            .SumAsync(i => (decimal?)((i.TotalAmount - i.AmountPaid) * i.ExchangeRate), ct) ?? 0m;

        var limit = customer.CreditLimit;
        var hasLimit = limit > 0m;
        var available = hasLimit ? Math.Max(0m, limit - outstanding) : 0m;
        var isOver = hasLimit && outstanding > limit;
        var enforced = _credit.Enabled && _credit.BlockOverLimit && hasLimit;

        return ApiResponse<CustomerCreditStatusDto>.Ok(new CustomerCreditStatusDto(
            req.CustomerId, limit, outstanding, available, isOver, enforced));
    }
}
