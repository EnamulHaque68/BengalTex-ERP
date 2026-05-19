using BengalTex.ERP.Application.VatChallan.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.VatChallan.Queries;

public sealed record GetVatChallanByIdQuery(long Id) : IRequest<ApiResponse<VatChallanDto>>;

internal sealed class GetVatChallanByIdQueryHandler
    : IRequestHandler<GetVatChallanByIdQuery, ApiResponse<VatChallanDto>>
{
    private readonly IRepository<Domain.Entities.VatChallan, long> _repo;

    public GetVatChallanByIdQueryHandler(IRepository<Domain.Entities.VatChallan, long> repo)
        => _repo = repo;

    public async Task<ApiResponse<VatChallanDto>> Handle(
        GetVatChallanByIdQuery request, CancellationToken cancellationToken)
    {
        var ch = await _repo.Query()
            .AsNoTracking()
            .Include(v => v.CustomerInvoice).ThenInclude(c => c.Customer)
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (ch is null) return ApiResponse<VatChallanDto>.Fail("VAT challan not found.");

        var dto = new VatChallanDto(
            ch.Id, ch.Code,
            ch.CustomerInvoiceId, ch.CustomerInvoice.Code,
            ch.CustomerInvoice.CustomerId, ch.CustomerInvoice.Customer.Name,
            ch.ChallanDate,
            ch.SubtotalAmount, ch.VatAmount, ch.TotalAmount,
            ch.Notes);

        return ApiResponse<VatChallanDto>.Ok(dto);
    }
}
