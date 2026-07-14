using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Payroll.Commands;

public sealed record DeletePayslipCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeletePayslipCommandHandler
    : IRequestHandler<DeletePayslipCommand, ApiResponse>
{
    private readonly IRepository<Payslip, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeletePayslipCommandHandler(IRepository<Payslip, long> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeletePayslipCommand cmd, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(cmd.Id, ct);
        if (p is null) return ApiResponse.Fail("Payslip not found.");
        // Only a Draft is deletable — Approved/Paid already carry posted journals (accrual/payment).
        if (p.Status != PayslipStatus.Draft)
            return ApiResponse.Fail($"A {p.Status} payslip cannot be deleted (its salary is already accrued).");

        _repo.Remove(p);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Payslip deleted.");
    }
}
