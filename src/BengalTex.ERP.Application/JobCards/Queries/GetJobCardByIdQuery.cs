using BengalTex.ERP.Application.JobCards.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.JobCards.Queries;

public sealed record GetJobCardByIdQuery(long Id) : IRequest<ApiResponse<JobCardDto>>;

internal sealed class GetJobCardByIdQueryHandler
    : IRequestHandler<GetJobCardByIdQuery, ApiResponse<JobCardDto>>
{
    private readonly IRepository<JobCard, long> _repo;
    public GetJobCardByIdQueryHandler(IRepository<JobCard, long> repo) => _repo = repo;

    public async Task<ApiResponse<JobCardDto>> Handle(GetJobCardByIdQuery request, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .AsNoTracking()
            .Where(j => j.Id == request.Id)
            .Select(j => new
            {
                j.Id,
                j.Code,
                j.ProductionOrderId,
                ProductionOrderCode = j.ProductionOrder.Code,
                ProductName = j.ProductionOrder.Product.Name,
                j.ProductionStageId,
                StageName = j.ProductionStage != null ? j.ProductionStage.StageName : null,
                j.BatchNumber,
                j.Quantity,
                j.CompletedQuantity,
                j.RejectedQuantity,
                j.MachineId,
                MachineCode = j.Machine != null ? j.Machine.Code : null,
                MachineName = j.Machine != null ? j.Machine.Name : null,
                j.OperatorEmployeeId,
                OperatorCode = j.OperatorEmployee != null ? j.OperatorEmployee.Code : null,
                OperatorName = j.OperatorEmployee != null ? j.OperatorEmployee.FullName : null,
                j.Status,
                j.StartedAt,
                j.LastResumedAt,
                j.CompletedAt,
                j.CompletedBy,
                j.ActiveMinutes,
                j.Notes,
                Scans = j.Scans.OrderBy(s => s.ScannedAt).Select(s => new JobCardScanDto(
                    s.Id, s.ScanType.ToString(), s.ScannedAt, s.ScannedBy,
                    s.Quantity, s.RejectedQuantity, s.Notes)).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (dto is null) return ApiResponse<JobCardDto>.Fail("Job card not found.");

        return ApiResponse<JobCardDto>.Ok(new JobCardDto(
            dto.Id, dto.Code, dto.ProductionOrderId, dto.ProductionOrderCode, dto.ProductName,
            dto.ProductionStageId, dto.StageName, dto.BatchNumber,
            dto.Quantity, dto.CompletedQuantity, dto.RejectedQuantity,
            dto.MachineId, dto.MachineCode, dto.MachineName,
            dto.OperatorEmployeeId, dto.OperatorCode, dto.OperatorName,
            dto.Status.ToString(), dto.StartedAt, dto.LastResumedAt, dto.CompletedAt,
            dto.CompletedBy, dto.ActiveMinutes, dto.Notes, dto.Scans));
    }
}
