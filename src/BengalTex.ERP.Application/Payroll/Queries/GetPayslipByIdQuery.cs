using BengalTex.ERP.Application.Payroll.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payroll.Queries;

public sealed record GetPayslipByIdQuery(long Id) : IRequest<ApiResponse<PayslipDto>>;

internal sealed class GetPayslipByIdQueryHandler
    : IRequestHandler<GetPayslipByIdQuery, ApiResponse<PayslipDto>>
{
    private readonly IRepository<Payslip, long> _repo;

    public GetPayslipByIdQueryHandler(IRepository<Payslip, long> repo) => _repo = repo;

    public async Task<ApiResponse<PayslipDto>> Handle(GetPayslipByIdQuery request, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new PayslipDto(
                p.Id, p.EmployeeId, p.Employee.Code, p.Employee.FullName,
                p.Year, p.Month, p.BasicSalary,
                p.PresentDays, p.AbsentDays, p.LeaveDays, p.OvertimeHours,
                p.OvertimeAmount, p.Allowances, p.Deductions, p.GrossPay, p.NetPay,
                p.Status.ToString(), p.PaidAt, p.Notes))
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? ApiResponse<PayslipDto>.Fail("Payslip not found.")
            : ApiResponse<PayslipDto>.Ok(dto);
    }
}
