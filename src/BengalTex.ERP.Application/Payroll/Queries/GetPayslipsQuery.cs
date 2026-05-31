using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Payroll.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payroll.Queries;

/// <summary>Paginated payslips, filtered by year/month, employee, and/or status.</summary>
public sealed record GetPayslipsQuery(
    PagedQueryParameters Parameters,
    int? Year = null,
    int? Month = null,
    int? EmployeeId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<PayslipDto>>>;

internal sealed class GetPayslipsQueryHandler
    : IRequestHandler<GetPayslipsQuery, ApiResponse<PagedResult<PayslipDto>>>
{
    private readonly IRepository<Payslip, long> _repo;

    public GetPayslipsQueryHandler(IRepository<Payslip, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<PayslipDto>>> Handle(
        GetPayslipsQuery request, CancellationToken ct)
    {
        var query = _repo.Query();

        if (request.Year.HasValue) query = query.Where(p => p.Year == request.Year.Value);
        if (request.Month.HasValue) query = query.Where(p => p.Month == request.Month.Value);
        if (request.EmployeeId.HasValue) query = query.Where(p => p.EmployeeId == request.EmployeeId.Value);
        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<PayslipStatus>(request.Status, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.Employee.Code.Contains(search) ||
                p.Employee.FullName.Contains(search));
        }

        query = query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ThenBy(p => p.Employee.FullName);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(p => new PayslipDto(
                p.Id, p.EmployeeId, p.Employee.Code, p.Employee.FullName,
                p.Year, p.Month, p.BasicSalary,
                p.PresentDays, p.AbsentDays, p.LeaveDays, p.OvertimeHours,
                p.OvertimeAmount, p.Allowances, p.Deductions,
                p.HouseRent, p.Medical, p.Transport, p.FoodAllowance, p.FestivalBonus,
                p.PfEmployee, p.PfEmployer, p.IncomeTax, p.LoanDeduction,
                p.GrossPay, p.NetPay,
                p.Status.ToString(), p.PaidAt, p.Notes))
            .ToListAsync(ct);

        var result = PagedResult<PayslipDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<PayslipDto>>.Ok(result);
    }
}
