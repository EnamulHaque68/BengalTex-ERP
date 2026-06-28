using BengalTex.ERP.Application.Receipt.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Receipt.Queries;

public sealed record GetReceiptByIdQuery(long Id) : IRequest<ApiResponse<ReceiptDto>>;

internal sealed class GetReceiptByIdQueryHandler
    : IRequestHandler<GetReceiptByIdQuery, ApiResponse<ReceiptDto>>
{
    private readonly IRepository<Domain.Entities.Receipt, long> _repo;

    public GetReceiptByIdQueryHandler(IRepository<Domain.Entities.Receipt, long> repo) => _repo = repo;

    public async Task<ApiResponse<ReceiptDto>> Handle(
        GetReceiptByIdQuery request, CancellationToken cancellationToken)
    {
        var rct = await _repo.Query()
            .AsNoTracking()
            .Include(r => r.CustomerInvoice).ThenInclude(c => c.Customer)
            .Include(r => r.CustomerInvoice).ThenInclude(c => c.Currency)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (rct is null) return ApiResponse<ReceiptDto>.Fail("Receipt not found.");

        var dto = new ReceiptDto(
            rct.Id, rct.Code,
            rct.CustomerInvoiceId, rct.CustomerInvoice.Code,
            rct.CustomerInvoice.CustomerId, rct.CustomerInvoice.Customer.Name,
            rct.ReceiptDate, rct.Amount, rct.ExchangeRate,
            rct.CustomerInvoice.Currency.Code, rct.Amount * rct.ExchangeRate,
            rct.Status.ToString(),
            rct.PaymentMethod.ToString(),
            rct.ReferenceNumber, rct.Notes);

        return ApiResponse<ReceiptDto>.Ok(dto);
    }
}
