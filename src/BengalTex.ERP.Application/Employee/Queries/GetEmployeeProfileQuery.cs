using System.Globalization;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Employee.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Employee.Queries;

/// <summary>Full profile for a specific employee (HR/admin view). Requires Employees.View.</summary>
public sealed record GetEmployeeProfileQuery(int EmployeeId) : IRequest<ApiResponse<EmployeeProfileDto>>;

/// <summary>The current user's own profile (self-service) — resolves their linked employee record.</summary>
public sealed record GetMyProfileQuery() : IRequest<ApiResponse<EmployeeProfileDto>>;

internal sealed class GetEmployeeProfileQueryHandler
    : IRequestHandler<GetEmployeeProfileQuery, ApiResponse<EmployeeProfileDto>>,
      IRequestHandler<GetMyProfileQuery, ApiResponse<EmployeeProfileDto>>
{
    private readonly IRepository<Domain.Entities.Employee> _repo;
    private readonly IRepository<LeaveBalance> _leaveRepo;
    private readonly IRepository<Payslip, long> _payslipRepo;
    private readonly IRepository<AttendanceRecord, long> _attendanceRepo;
    private readonly IRepository<EmployeeSkill> _skillRepo;
    private readonly IRepository<EmployeeEducation> _eduRepo;
    private readonly IRepository<EmployeeEmergencyContact> _contactRepo;
    private readonly ICurrentUserService _currentUser;

    public GetEmployeeProfileQueryHandler(
        IRepository<Domain.Entities.Employee> repo, IRepository<LeaveBalance> leaveRepo,
        IRepository<Payslip, long> payslipRepo, IRepository<AttendanceRecord, long> attendanceRepo,
        IRepository<EmployeeSkill> skillRepo, IRepository<EmployeeEducation> eduRepo,
        IRepository<EmployeeEmergencyContact> contactRepo, ICurrentUserService currentUser)
    { _repo = repo; _leaveRepo = leaveRepo; _payslipRepo = payslipRepo; _attendanceRepo = attendanceRepo; _skillRepo = skillRepo; _eduRepo = eduRepo; _contactRepo = contactRepo; _currentUser = currentUser; }

    public Task<ApiResponse<EmployeeProfileDto>> Handle(GetEmployeeProfileQuery req, CancellationToken ct)
        => BuildAsync(e => e.Id == req.EmployeeId, ct);

    public Task<ApiResponse<EmployeeProfileDto>> Handle(GetMyProfileQuery req, CancellationToken ct)
    {
        var uid = _currentUser.UserId;
        const string notLinked = "Your login isn't linked to an employee record yet. An admin can link it from the employee's profile (Edit Profile → Login Account).";
        if (string.IsNullOrEmpty(uid))
            return Task.FromResult(ApiResponse<EmployeeProfileDto>.Fail(notLinked));
        return BuildAsync(e => e.UserId == uid, ct, notLinked);
    }

    private async Task<ApiResponse<EmployeeProfileDto>> BuildAsync(
        System.Linq.Expressions.Expression<Func<Domain.Entities.Employee, bool>> predicate, CancellationToken ct,
        string? notFoundMessage = null)
    {
        var e = await _repo.Query().AsNoTracking()
            .Include(x => x.BankAccount)
            .Include(x => x.ReportingTo)
            .FirstOrDefaultAsync(predicate, ct);
        if (e is null) return ApiResponse<EmployeeProfileDto>.Fail(notFoundMessage ?? "Employee profile not found.");

        var year = DateTime.UtcNow.Year;
        var balances = await _leaveRepo.Query().AsNoTracking()
            .Where(b => b.EmployeeId == e.Id && b.Year == year)
            .Include(b => b.LeaveType)
            .OrderBy(b => b.LeaveType.Name)
            .Select(b => new ProfileLeaveBalanceDto(b.LeaveType.Name, b.Entitled, b.Taken, b.Entitled - b.Taken))
            .ToListAsync(ct);

        var payslips = await _payslipRepo.Query().AsNoTracking()
            .Where(p => p.EmployeeId == e.Id)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Take(6)
            .Select(p => new { p.Id, p.Year, p.Month, p.NetPay, p.Status })
            .ToListAsync(ct);

        var payslipDtos = payslips.Select(p => new ProfilePayslipDto(
            p.Id, p.Year, p.Month,
            CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(p.Month) + " " + p.Year,
            p.NetPay, p.Status.ToString())).ToList();

        var skills = await _skillRepo.Query().AsNoTracking()
            .Where(s => s.EmployeeId == e.Id)
            .OrderBy(s => s.SortOrder).ThenByDescending(s => s.ProficiencyPercent)
            .Select(s => new ProfileSkillDto(s.Id, s.Name, s.ProficiencyPercent))
            .ToListAsync(ct);

        var education = await _eduRepo.Query().AsNoTracking()
            .Where(x => x.EmployeeId == e.Id)
            .OrderBy(x => x.SortOrder).ThenByDescending(x => x.PassingYear)
            .Select(x => new ProfileEducationDto(x.Id, x.Degree, x.Institute, x.PassingYear, x.Result))
            .ToListAsync(ct);

        var contacts = await _contactRepo.Query().AsNoTracking()
            .Where(x => x.EmployeeId == e.Id)
            .OrderBy(x => x.SortOrder)
            .Select(x => new ProfileEmergencyContactDto(x.Id, x.Name, x.Relationship, x.Phone, x.Address))
            .ToListAsync(ct);

        // Current-month attendance breakdown for the donut
        var now = DateTime.UtcNow;
        var monthStart = new DateOnly(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var att = await _attendanceRepo.Query().AsNoTracking()
            .Where(a => a.EmployeeId == e.Id && a.AttendanceDate >= monthStart && a.AttendanceDate < monthEnd)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        int Count(AttendanceStatus s) => att.FirstOrDefault(x => x.Status == s)?.Count ?? 0;
        var present = Count(AttendanceStatus.Present);
        var late = Count(AttendanceStatus.Late);
        var absent = Count(AttendanceStatus.Absent);
        var leave = Count(AttendanceStatus.Leave);
        var attendance = new ProfileAttendanceSummaryDto(
            now.Year, now.Month, CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(now.Month) + " " + now.Year,
            present, late, absent, leave, present + late + absent + leave);

        var gross = e.BasicSalary + e.HouseRentAllowance + e.MedicalAllowance + e.TransportAllowance + e.FoodAllowance;
        var canEdit = _currentUser.HasPermission(Permissions.Employees.Edit);

        var dto = new EmployeeProfileDto(
            e.Id, e.Code, e.FullName, e.Designation, e.Department, e.PhotoUrl, e.IsActive, e.Status.ToString(),
            e.JoiningDate, e.EmploymentType.ToString(), e.WorkLocation,
            e.Email, e.Phone, e.DateOfBirth, e.Nationality, e.BloodGroup, e.Gender.ToString(),
            e.MaritalStatus.ToString(), e.Religion, e.NationalId, e.Address, e.AboutMe,
            e.ReportingToEmployeeId, e.ReportingTo != null ? e.ReportingTo.FullName : null,
            e.ProbationEndDate, e.ConfirmationDate,
            e.BasicSalary, e.HouseRentAllowance, e.MedicalAllowance, e.TransportAllowance, e.FoodAllowance, gross,
            e.BankAccount != null ? e.BankAccount.BankName : null,
            e.BankAccount != null ? MaskAccount(e.BankAccount.AccountNumber) : null,
            balances, payslipDtos, skills, education, contacts, attendance, e.UserId, canEdit);

        return ApiResponse<EmployeeProfileDto>.Ok(dto);
    }

    private static string MaskAccount(string acct) =>
        string.IsNullOrEmpty(acct) || acct.Length <= 4 ? acct : new string('*', acct.Length - 4) + acct[^4..];
}
