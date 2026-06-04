using System.Globalization;
using BengalTex.ERP.Application.Payroll.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payroll.Queries;

/// <summary>
/// Returns an enriched payslip view for the printable salary-slip page — joins Employee
/// (+ Department + Designation + BankAccount) onto the payslip data and pulls the Company
/// record for header/footer rendering. Single round-trip via EF projections.
/// </summary>
public sealed record GetPayslipForPrintQuery(long Id) : IRequest<ApiResponse<PayslipPrintDto>>;

internal sealed class GetPayslipForPrintQueryHandler
    : IRequestHandler<GetPayslipForPrintQuery, ApiResponse<PayslipPrintDto>>
{
    private readonly IRepository<Payslip, long> _payslipRepo;
    private readonly IRepository<Domain.Entities.Company> _companyRepo;

    public GetPayslipForPrintQueryHandler(
        IRepository<Payslip, long> payslipRepo,
        IRepository<Domain.Entities.Company> companyRepo)
    {
        _payslipRepo = payslipRepo;
        _companyRepo = companyRepo;
    }

    public async Task<ApiResponse<PayslipPrintDto>> Handle(GetPayslipForPrintQuery q, CancellationToken ct)
    {
        var company = await _companyRepo.Query().AsNoTracking().FirstOrDefaultAsync(ct);

        var slip = await _payslipRepo.Query().AsNoTracking()
            .Include(p => p.Employee).ThenInclude(e => e.DepartmentEntity)
            .Include(p => p.Employee).ThenInclude(e => e.DesignationEntity)
            .Include(p => p.Employee).ThenInclude(e => e.BankAccount)
            .FirstOrDefaultAsync(p => p.Id == q.Id, ct);

        if (slip is null) return ApiResponse<PayslipPrintDto>.Fail("Payslip not found.");

        var e = slip.Employee;
        var otherDeductions = slip.Deductions - slip.PfEmployee - slip.IncomeTax - slip.LoanDeduction;
        if (otherDeductions < 0m) otherDeductions = 0m;

        var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(slip.Month);
        var code = $"PS-{slip.Year:0000}-{slip.Month:00}-{e.Code}";

        var dto = new PayslipPrintDto(
            slip.Id, code,
            e.Id, e.Code, e.FullName,
            e.DesignationEntity?.Name ?? e.Designation,
            e.DepartmentEntity?.Name ?? e.Department,
            e.Phone, e.NationalId,
            e.JoiningDate, e.EmploymentType.ToString(),
            slip.Year, slip.Month, monthName,
            slip.PresentDays, slip.AbsentDays, slip.LeaveDays, slip.OvertimeHours,
            slip.BasicSalary, slip.HouseRent, slip.Medical, slip.Transport,
            slip.FoodAllowance, slip.FestivalBonus, slip.Allowances, slip.OvertimeAmount,
            slip.GrossPay,
            slip.PfEmployee, slip.PfEmployer, slip.IncomeTax, slip.LoanDeduction,
            otherDeductions, slip.Deductions,
            slip.NetPay,
            slip.Status.ToString(), slip.PaidAt, slip.Notes,
            e.BankAccount?.BankName, e.BankAccount?.BranchName, e.BankAccount?.AccountNumber,
            company?.Name ?? "Bengal TEX",
            company?.ShortName,
            company?.AddressLine1, company?.AddressLine2,
            company?.City, company?.District, company?.PostalCode,
            company?.Phone, company?.Email,
            company?.TaxNumber, company?.LogoUrl);

        return ApiResponse<PayslipPrintDto>.Ok(dto);
    }
}
