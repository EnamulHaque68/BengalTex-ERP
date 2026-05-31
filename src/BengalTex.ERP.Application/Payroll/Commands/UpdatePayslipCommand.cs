using BengalTex.ERP.Application.Payroll.Dtos;
using BengalTex.ERP.Application.Payroll.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Payroll.Commands;

/// <summary>
/// Adjust a Draft payslip's overtime / allowances / BD components / deductions.
/// Gross + Net recompute from edited values.
///   Gross = Basic + Allowances + OvertimeAmount + HouseRent + Medical + Transport + FoodAllowance + FestivalBonus
///   Net   = Gross − (Deductions + PfEmployee + IncomeTax + LoanDeduction)
/// </summary>
public sealed record UpdatePayslipCommand(
    long Id,
    decimal OvertimeAmount,
    decimal Allowances,
    decimal Deductions,
    decimal HouseRent,
    decimal Medical,
    decimal Transport,
    decimal FoodAllowance,
    decimal FestivalBonus,
    decimal PfEmployee,
    decimal PfEmployer,
    decimal IncomeTax,
    decimal LoanDeduction,
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
        RuleFor(x => x.HouseRent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Medical).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Transport).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FoodAllowance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FestivalBonus).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PfEmployee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PfEmployer).GreaterThanOrEqualTo(0);
        RuleFor(x => x.IncomeTax).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LoanDeduction).GreaterThanOrEqualTo(0);
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
        p.HouseRent = cmd.HouseRent;
        p.Medical = cmd.Medical;
        p.Transport = cmd.Transport;
        p.FoodAllowance = cmd.FoodAllowance;
        p.FestivalBonus = cmd.FestivalBonus;
        p.PfEmployee = cmd.PfEmployee;
        p.PfEmployer = cmd.PfEmployer;
        p.IncomeTax = cmd.IncomeTax;
        p.LoanDeduction = cmd.LoanDeduction;

        var totalDeductions = Round(cmd.Deductions + cmd.PfEmployee + cmd.IncomeTax + cmd.LoanDeduction);
        p.Deductions = totalDeductions;
        p.GrossPay = Round(p.BasicSalary + p.Allowances + p.OvertimeAmount
                          + p.HouseRent + p.Medical + p.Transport + p.FoodAllowance + p.FestivalBonus);
        p.NetPay = Round(p.GrossPay - p.Deductions);
        p.Notes = cmd.Notes;

        _repo.Update(p);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetPayslipByIdQuery(p.Id), ct);
    }

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}
