using BengalTex.ERP.Application.Payment.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payment.Queries;

public sealed record GetPaymentByIdQuery(long Id) : IRequest<ApiResponse<PaymentDto>>;

internal sealed class GetPaymentByIdQueryHandler
    : IRequestHandler<GetPaymentByIdQuery, ApiResponse<PaymentDto>>
{
    private readonly IRepository<Domain.Entities.Payment, long> _repo;

    public GetPaymentByIdQueryHandler(IRepository<Domain.Entities.Payment, long> repo) => _repo = repo;

    public async Task<ApiResponse<PaymentDto>> Handle(
        GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var pay = await _repo.Query()
            .AsNoTracking()
            .Include(p => p.SupplierInvoice).ThenInclude(s => s.Supplier)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (pay is null) return ApiResponse<PaymentDto>.Fail("Payment not found.");

        var dto = new PaymentDto(
            pay.Id, pay.Code,
            pay.SupplierInvoiceId, pay.SupplierInvoice.Code,
            pay.SupplierInvoice.SupplierId, pay.SupplierInvoice.Supplier.Name,
            pay.PaymentDate, pay.Amount, pay.ExchangeRate,
            pay.PaymentMethod.ToString(),
            pay.ReferenceNumber, pay.AitAmount, pay.VdsAmount, pay.Notes);

        return ApiResponse<PaymentDto>.Ok(dto);
    }
}
