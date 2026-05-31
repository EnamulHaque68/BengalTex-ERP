using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.JobCards.Commands;

/// <summary>Edit only allowed while the card is Open (not yet started).</summary>
public sealed record UpdateJobCardCommand(
    long Id,
    string? BatchNumber,
    decimal Quantity,
    int? MachineId,
    int? OperatorEmployeeId,
    string? Notes
) : IRequest<ApiResponse>;

public sealed class UpdateJobCardCommandValidator : AbstractValidator<UpdateJobCardCommand>
{
    public UpdateJobCardCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.BatchNumber).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateJobCardCommandHandler : IRequestHandler<UpdateJobCardCommand, ApiResponse>
{
    private readonly IRepository<JobCard, long> _repo;
    private readonly IRepository<Machine> _machineRepo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;

    public UpdateJobCardCommandHandler(
        IRepository<JobCard, long> repo,
        IRepository<Machine> machineRepo,
        IRepository<Domain.Entities.Employee> empRepo,
        IUnitOfWork uow)
    {
        _repo = repo; _machineRepo = machineRepo; _empRepo = empRepo; _uow = uow;
    }

    public async Task<ApiResponse> Handle(UpdateJobCardCommand cmd, CancellationToken ct)
    {
        var jc = await _repo.GetByIdAsync(cmd.Id, ct);
        if (jc is null) return ApiResponse.Fail("Job card not found.");
        if (jc.Status != JobCardStatus.Open)
            return ApiResponse.Fail($"Cannot edit a {jc.Status} job card.");

        if (cmd.MachineId is int mid && !await _machineRepo.Query().AnyAsync(m => m.Id == mid && m.IsActive, ct))
            return ApiResponse.Fail("Machine not found or inactive.");
        if (cmd.OperatorEmployeeId is int eid && !await _empRepo.Query().AnyAsync(e => e.Id == eid && e.IsActive, ct))
            return ApiResponse.Fail("Operator not found or inactive.");

        jc.BatchNumber = string.IsNullOrWhiteSpace(cmd.BatchNumber) ? null : cmd.BatchNumber.Trim();
        jc.Quantity = cmd.Quantity;
        jc.MachineId = cmd.MachineId;
        jc.OperatorEmployeeId = cmd.OperatorEmployeeId;
        jc.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();

        _repo.Update(jc);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Job card updated.");
    }
}

public sealed record DeleteJobCardCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteJobCardCommandHandler : IRequestHandler<DeleteJobCardCommand, ApiResponse>
{
    private readonly IRepository<JobCard, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteJobCardCommandHandler(IRepository<JobCard, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteJobCardCommand cmd, CancellationToken ct)
    {
        var jc = await _repo.GetByIdAsync(cmd.Id, ct);
        if (jc is null) return ApiResponse.Fail("Job card not found.");
        if (jc.Status != JobCardStatus.Open)
            return ApiResponse.Fail($"Cannot delete a {jc.Status} job card (cancel it instead).");
        _repo.Remove(jc);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Job card deleted.");
    }
}
