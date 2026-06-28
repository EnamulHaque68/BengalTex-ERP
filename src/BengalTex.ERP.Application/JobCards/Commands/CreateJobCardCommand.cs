using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.JobCards.Commands;

public sealed record CreateJobCardCommand(
    long ProductionOrderId,
    long? ProductionStageId,
    string? BatchNumber,
    decimal Quantity,
    int? MachineId,
    int? OperatorEmployeeId,
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class CreateJobCardCommandValidator : AbstractValidator<CreateJobCardCommand>
{
    public CreateJobCardCommandValidator()
    {
        RuleFor(x => x.ProductionOrderId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.BatchNumber).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateJobCardCommandHandler : IRequestHandler<CreateJobCardCommand, ApiResponse<long>>
{
    private readonly IRepository<JobCard, long> _repo;
    private readonly IRepository<ProductionOrder, long> _poRepo;
    private readonly IRepository<ProductionStage, long> _stageRepo;
    private readonly IRepository<Machine> _machineRepo;
    private readonly IRepository<Domain.Entities.MachineMaintenance, long> _maintRepo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateJobCardCommandHandler(
        IRepository<JobCard, long> repo,
        IRepository<ProductionOrder, long> poRepo,
        IRepository<ProductionStage, long> stageRepo,
        IRepository<Machine> machineRepo,
        IRepository<Domain.Entities.MachineMaintenance, long> maintRepo,
        IRepository<Domain.Entities.Employee> empRepo,
        IUnitOfWork uow,
        INumberingService numbering)
    {
        _repo = repo; _poRepo = poRepo; _stageRepo = stageRepo;
        _machineRepo = machineRepo; _maintRepo = maintRepo; _empRepo = empRepo;
        _uow = uow; _numbering = numbering;
    }

    public async Task<ApiResponse<long>> Handle(CreateJobCardCommand cmd, CancellationToken ct)
    {
        var po = await _poRepo.GetByIdAsync(cmd.ProductionOrderId, ct);
        if (po is null) return ApiResponse<long>.Fail("Production order not found.");
        if (po.Status == ProductionOrderStatus.Cancelled || po.Status == ProductionOrderStatus.Completed)
            return ApiResponse<long>.Fail($"Cannot create job cards on a {po.Status} production order.");

        if (cmd.ProductionStageId is long stageId)
        {
            var stage = await _stageRepo.Query().FirstOrDefaultAsync(s => s.Id == stageId, ct);
            if (stage is null || stage.ProductionOrderId != po.Id)
                return ApiResponse<long>.Fail("Stage does not belong to this production order.");
        }
        if (cmd.MachineId is int mid && !await _machineRepo.Query().AnyAsync(m => m.Id == mid && m.IsActive, ct))
            return ApiResponse<long>.Fail("Machine not found or inactive.");

        // Phase 5 — machine availability gate: block assigning a machine currently under maintenance.
        if (cmd.MachineId is int gmid && await _maintRepo.Query()
                .AnyAsync(m => m.MachineId == gmid && m.Status == MaintenanceStatus.InProgress, ct))
            return ApiResponse<long>.Fail("This machine is under maintenance (in progress). Assign a different machine or wait for the maintenance to complete.");
        if (cmd.OperatorEmployeeId is int eid && !await _empRepo.Query().AnyAsync(e => e.Id == eid && e.IsActive, ct))
            return ApiResponse<long>.Fail("Operator not found or inactive.");

        var code = await _numbering.NextAsync("JC", null, ct);

        var jc = new JobCard
        {
            Code = code,
            ProductionOrderId = po.Id,
            ProductionStageId = cmd.ProductionStageId,
            BatchNumber = string.IsNullOrWhiteSpace(cmd.BatchNumber) ? null : cmd.BatchNumber.Trim(),
            Quantity = cmd.Quantity,
            MachineId = cmd.MachineId,
            OperatorEmployeeId = cmd.OperatorEmployeeId,
            Status = JobCardStatus.Open,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(jc, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(jc.Id, "Job card created.");
    }
}
