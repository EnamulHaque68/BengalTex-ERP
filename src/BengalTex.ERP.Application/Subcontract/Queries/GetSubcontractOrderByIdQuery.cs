using BengalTex.ERP.Application.Subcontract.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Subcontract.Queries;

public sealed record GetSubcontractOrderByIdQuery(long Id) : IRequest<ApiResponse<SubcontractOrderDto>>;

internal sealed class GetSubcontractOrderByIdQueryHandler
    : IRequestHandler<GetSubcontractOrderByIdQuery, ApiResponse<SubcontractOrderDto>>
{
    private readonly IRepository<SubcontractOrder, long> _repo;

    public GetSubcontractOrderByIdQueryHandler(IRepository<SubcontractOrder, long> repo) => _repo = repo;

    public async Task<ApiResponse<SubcontractOrderDto>> Handle(
        GetSubcontractOrderByIdQuery request, CancellationToken ct)
    {
        var o = await _repo.Query()
            .AsNoTracking()
            .Include(s => s.Subcontractor)
            .Include(s => s.Warehouse)
            .Include(s => s.Lines).ThenInclude(l => l.RawMaterial).ThenInclude(rm => rm!.UnitOfMeasure)
            .Include(s => s.Lines).ThenInclude(l => l.Product).ThenInclude(p => p!.UnitOfMeasure)
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (o is null) return ApiResponse<SubcontractOrderDto>.Fail("Subcontract order not found.");

        var lines = o.Lines.OrderBy(l => l.SortOrder).Select(l => new SubcontractLineDto(
            l.Id,
            l.RawMaterialId,
            l.ProductId,
            l.RawMaterialId.HasValue ? "RawMaterial" : "Product",
            l.RawMaterialId.HasValue ? l.RawMaterial!.Code : l.Product!.Code,
            l.RawMaterialId.HasValue ? l.RawMaterial!.Name : l.Product!.Name,
            l.RawMaterialId.HasValue ? l.RawMaterial!.UnitOfMeasure.Code : l.Product!.UnitOfMeasure.Code,
            l.IssuedQuantity, l.ReceivedQuantity, l.SortOrder, l.LineNotes)).ToList();

        var dto = new SubcontractOrderDto(
            o.Id, o.Code, o.SubcontractorId, o.Subcontractor.Name,
            o.OrderDate, o.ExpectedReturnDate, o.ProcessType,
            o.WarehouseId, o.Warehouse.Name, o.Status.ToString(), o.ChargeAmount,
            o.IssuedAt, o.IssuedBy, o.ReceivedAt, o.ReceivedBy, o.Notes, lines);

        return ApiResponse<SubcontractOrderDto>.Ok(dto);
    }
}
