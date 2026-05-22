using BengalTex.ERP.Application.Payroll.Dtos;
using BengalTex.ERP.Application.Payroll.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Payroll.Commands;

/// <summary>Adjust a Draft payslip's allowances / overtime / deductions; Gross + Net recompute.</summary>
public sealed record UpdatePayslipCommand(
    long Id,
    decimal OvertimeAmount,
    decimal Allowances,
    decimal Deductions,
    string? Notes
) : IRequest<ApiResponse<PayslipDto>>;

public sealed class UpdatePayslipCommandValidator : AbstractValidator<UpdatePayslipCommand>
{
    public UpdatePayslipCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.OvertimeAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Allowances).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Deductions).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class UpdatePayslipCommandHandler
    : IRequestHandler<UpdatePayslipCommand, ApiResponse<PayslipDto>>
{
    private readonly IRepository<Payslip, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdatePayslipCommandHandler(IRepository<Payslip, long> repo, IUnitOfWork uow, IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<PayslipDto>> Handle(UpdatePayslipCommand cmd, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(cmd.Id, ct);
        if (p is null) return ApiResponse<PayslipDto>.Fail("Payslip not found.");
        if (p.Status != PayslipStatus.Draft)
            return ApiResponse<PayslipDto>.Fail("Only draft payslips can be adjusted.");

        p.OvertimeAmount = cmd.OvertimeAmount;
        p.Allowances = cmd.Allowances;
        p.Deductions = cmd.Deductions;
        p.GrossPay = Math.Round(p.BasicSalary + p.Allowances + p.OvertimeAmount, 2, MidpointRounding.AwayFromZero);
        p.NetPay = Math.Round(p.GrossPay - p.Deductions, 2, MidpointRounding.AwayFromZero);
        p.Notes = cmd.Notes;

        _repo.Update(p);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetPayslipByIdQuery(p.Id), ct);
    }
}
