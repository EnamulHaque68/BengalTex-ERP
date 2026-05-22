using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Payroll.Dtos;
using BengalTex.ERP.Application.Payroll.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Payroll.Commands;

/// <summary>Marks a Draft payslip as Paid (immutable thereafter).</summary>
public sealed record MarkPayslipPaidCommand(long Id) : IRequest<ApiResponse<PayslipDto>>;

internal sealed class MarkPayslipPaidCommandHandler
    : IRequestHandler<MarkPayslipPaidCommand, ApiResponse<PayslipDto>>
{
    private readonly IRepository<Payslip, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public MarkPayslipPaidCommandHandler(
        IRepository<Payslip, long> repo, IUnitOfWork uow, ICurrentUserService currentUser, IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<PayslipDto>> Handle(MarkPayslipPaidCommand cmd, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(cmd.Id, ct);
        if (p is null) return ApiResponse<PayslipDto>.Fail("Payslip not found.");
        if (p.Status == PayslipStatus.Paid)
            return ApiResponse<PayslipDto>.Fail("Payslip is already marked paid.");

        p.Status = PayslipStatus.Paid;
        p.PaidAt = DateTimeOffset.UtcNow;
        p.PaidBy = _currentUser.UserName;

        _repo.Update(p);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetPayslipByIdQuery(p.Id), ct);
    }
}
