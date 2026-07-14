using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Payroll.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payroll.Commands;

// ───────────────────────────────────────────────────────────────────────────
//   Calculate Preview  (read-only, auto-fills the create form)
// ───────────────────────────────────────────────────────────────────────────
public sealed record CalculateFinalSettlementQuery(int EmployeeId, DateOnly LastWorkingDate)
    : IRequest<ApiResponse<FinalSettlementPreviewDto>>;

internal sealed class CalculateFinalSettlementQueryHandler
    : IRequestHandler<CalculateFinalSettlementQuery, ApiResponse<FinalSettlementPreviewDto>>
{
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IRepository<LeaveBalance> _leaveBalRepo;
    private readonly IRepository<EmployeeLoan, long> _loanRepo;

    public CalculateFinalSettlementQueryHandler(
        IRepository<Domain.Entities.Employee> empRepo,
        IRepository<LeaveBalance> leaveBalRepo,
        IRepository<EmployeeLoan, long> loanRepo)
    {
        _empRepo = empRepo;
        _leaveBalRepo = leaveBalRepo;
        _loanRepo = loanRepo;
    }

    public async Task<ApiResponse<FinalSettlementPreviewDto>> Handle(
        CalculateFinalSettlementQuery req, CancellationToken ct)
    {
        var emp = await _empRepo.GetByIdAsync(req.EmployeeId, ct);
        if (emp is null) return ApiResponse<FinalSettlementPreviewDto>.Fail("Employee not found.");
        if (req.LastWorkingDate < emp.JoiningDate)
            return ApiResponse<FinalSettlementPreviewDto>.Fail("Last working date cannot be before joining date.");

        var basic = emp.BasicSalary;
        var perDay = basic / 30m;

        // Prorated salary: days worked in the final month
        var monthStart = new DateOnly(req.LastWorkingDate.Year, req.LastWorkingDate.Month, 1);
        var proratedDays = req.LastWorkingDate.Day; // simplistic: assume each month = 30 working days
        var proratedSalary = Round(perDay * proratedDays);

        // Leave encashment: sum of remaining balance from PAID leave types (current year)
        var year = req.LastWorkingDate.Year;
        var leaveDays = await _leaveBalRepo.Query()
            .Where(b => b.EmployeeId == emp.Id && b.Year == year && b.LeaveType.IsPaid)
            .SumAsync(b => (decimal?)(b.Entitled - b.Taken), ct) ?? 0m;
        if (leaveDays < 0m) leaveDays = 0m;
        var leaveAmount = Round(perDay * leaveDays);

        // Gratuity (BD Labour Act 2006)
        var grat = GratuityCalculator.Calculate(emp.JoiningDate, req.LastWorkingDate, basic);

        // Outstanding loans
        var outstandingLoan = await _loanRepo.Query()
            .Where(l => l.EmployeeId == emp.Id && l.Status == EmployeeLoanStatus.Active)
            .SumAsync(l => (decimal?)l.OutstandingPrincipal, ct) ?? 0m;

        var gross = Round(proratedSalary + leaveAmount + grat.Amount);
        var totalDed = Round(outstandingLoan);
        var net = Round(gross - totalDed);

        return ApiResponse<FinalSettlementPreviewDto>.Ok(new FinalSettlementPreviewDto(
            emp.Id, emp.Code, emp.FullName, emp.JoiningDate, basic, req.LastWorkingDate,
            grat.Years, proratedDays, proratedSalary,
            leaveDays, leaveAmount, grat.Amount, outstandingLoan,
            gross, totalDed, net));
    }

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}

// ───────────────────────────────────────────────────────────────────────────
//   List
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetFinalSettlementsQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    int? EmployeeId = null
) : IRequest<ApiResponse<PagedResult<FinalSettlementDto>>>;

internal sealed class GetFinalSettlementsQueryHandler
    : IRequestHandler<GetFinalSettlementsQuery, ApiResponse<PagedResult<FinalSettlementDto>>>
{
    private readonly IRepository<FinalSettlement, long> _repo;
    public GetFinalSettlementsQueryHandler(IRepository<FinalSettlement, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<FinalSettlementDto>>> Handle(
        GetFinalSettlementsQuery req, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!string.IsNullOrEmpty(req.Status)
            && Enum.TryParse<FinalSettlementStatus>(req.Status, out var s))
            q = q.Where(x => x.Status == s);
        if (req.EmployeeId.HasValue) q = q.Where(x => x.EmployeeId == req.EmployeeId.Value);

        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.Code.Contains(search) ||
                             x.Employee.Code.Contains(search) ||
                             x.Employee.FullName.Contains(search));

        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((req.Parameters.Page - 1) * req.Parameters.PageSize)
            .Take(req.Parameters.PageSize)
            .Select(x => new FinalSettlementDto(
                x.Id, x.Code, x.EmployeeId, x.Employee.Code, x.Employee.FullName,
                x.SettlementDate, x.LastWorkingDate, x.JoiningDate, x.YearsOfService,
                x.Reason.ToString(),
                x.BasicSalary, x.ProratedDays, x.ProratedSalary,
                x.LeaveEncashmentDays, x.LeaveEncashmentAmount,
                x.GratuityAmount, x.OtherEarnings,
                x.OutstandingLoan, x.OtherDeductions,
                x.GrossPayable, x.TotalDeductions, x.NetPayable,
                x.Status.ToString(),
                x.ApprovedAt, x.ApprovedByUser, x.PaidAt,
                x.PaymentMethod, x.PaymentReference, x.Notes))
            .ToListAsync(ct);

        return ApiResponse<PagedResult<FinalSettlementDto>>.Ok(
            PagedResult<FinalSettlementDto>.Create(items, req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Get By Id
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetFinalSettlementByIdQuery(long Id) : IRequest<ApiResponse<FinalSettlementDto>>;

internal sealed class GetFinalSettlementByIdQueryHandler
    : IRequestHandler<GetFinalSettlementByIdQuery, ApiResponse<FinalSettlementDto>>
{
    private readonly IRepository<FinalSettlement, long> _repo;
    public GetFinalSettlementByIdQueryHandler(IRepository<FinalSettlement, long> repo) => _repo = repo;

    public async Task<ApiResponse<FinalSettlementDto>> Handle(GetFinalSettlementByIdQuery q, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .Where(x => x.Id == q.Id)
            .Select(x => new FinalSettlementDto(
                x.Id, x.Code, x.EmployeeId, x.Employee.Code, x.Employee.FullName,
                x.SettlementDate, x.LastWorkingDate, x.JoiningDate, x.YearsOfService,
                x.Reason.ToString(),
                x.BasicSalary, x.ProratedDays, x.ProratedSalary,
                x.LeaveEncashmentDays, x.LeaveEncashmentAmount,
                x.GratuityAmount, x.OtherEarnings,
                x.OutstandingLoan, x.OtherDeductions,
                x.GrossPayable, x.TotalDeductions, x.NetPayable,
                x.Status.ToString(),
                x.ApprovedAt, x.ApprovedByUser, x.PaidAt,
                x.PaymentMethod, x.PaymentReference, x.Notes))
            .FirstOrDefaultAsync(ct);
        return dto is null
            ? ApiResponse<FinalSettlementDto>.Fail("Settlement not found.")
            : ApiResponse<FinalSettlementDto>.Ok(dto);
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Create Draft
// ───────────────────────────────────────────────────────────────────────────
public sealed record CreateFinalSettlementCommand(
    int EmployeeId,
    DateOnly LastWorkingDate,
    DateOnly SettlementDate,
    string Reason,                // SettlementReason enum value
    decimal ProratedDays,
    decimal ProratedSalary,
    decimal LeaveEncashmentDays,
    decimal LeaveEncashmentAmount,
    decimal GratuityAmount,
    decimal OtherEarnings,
    decimal OutstandingLoan,
    decimal OtherDeductions,
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class CreateFinalSettlementCommandValidator : AbstractValidator<CreateFinalSettlementCommand>
{
    public CreateFinalSettlementCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty()
            .Must(r => Enum.TryParse<SettlementReason>(r, out _))
            .WithMessage("Reason must be one of: Resignation, Termination, Retirement, Death, EndOfContract.");
        RuleFor(x => x.ProratedDays).InclusiveBetween(0m, 31m);
        RuleFor(x => x.ProratedSalary).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LeaveEncashmentDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LeaveEncashmentAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.GratuityAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OtherEarnings).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OutstandingLoan).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OtherDeductions).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateFinalSettlementCommandHandler
    : IRequestHandler<CreateFinalSettlementCommand, ApiResponse<long>>
{
    private readonly IRepository<FinalSettlement, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateFinalSettlementCommandHandler(
        IRepository<FinalSettlement, long> repo,
        IRepository<Domain.Entities.Employee> empRepo,
        IUnitOfWork uow,
        INumberingService numbering)
    {
        _repo = repo; _empRepo = empRepo; _uow = uow; _numbering = numbering;
    }

    public async Task<ApiResponse<long>> Handle(CreateFinalSettlementCommand cmd, CancellationToken ct)
    {
        var emp = await _empRepo.GetByIdAsync(cmd.EmployeeId, ct);
        if (emp is null) return ApiResponse<long>.Fail("Employee not found.");
        if (cmd.LastWorkingDate < emp.JoiningDate)
            return ApiResponse<long>.Fail("Last working date cannot be before joining date.");

        // Guard: only one non-cancelled settlement per employee
        var hasActive = await _repo.Query()
            .AnyAsync(x => x.EmployeeId == cmd.EmployeeId && x.Status != FinalSettlementStatus.Cancelled, ct);
        if (hasActive) return ApiResponse<long>.Fail("This employee already has an active settlement (Draft/Approved/Paid). Cancel it first.");

        var grat = GratuityCalculator.Calculate(emp.JoiningDate, cmd.LastWorkingDate, emp.BasicSalary);
        var gross = Round(cmd.ProratedSalary + cmd.LeaveEncashmentAmount + cmd.GratuityAmount + cmd.OtherEarnings);
        var totalDed = Round(cmd.OutstandingLoan + cmd.OtherDeductions);
        var net = Round(gross - totalDed);

        var code = await _numbering.NextAsync("FS", null, ct);
        var entity = new FinalSettlement
        {
            Code = code,
            EmployeeId = cmd.EmployeeId,
            SettlementDate = cmd.SettlementDate,
            LastWorkingDate = cmd.LastWorkingDate,
            JoiningDate = emp.JoiningDate,
            YearsOfService = grat.Years,
            Reason = Enum.Parse<SettlementReason>(cmd.Reason),
            BasicSalary = emp.BasicSalary,
            ProratedDays = cmd.ProratedDays,
            ProratedSalary = cmd.ProratedSalary,
            LeaveEncashmentDays = cmd.LeaveEncashmentDays,
            LeaveEncashmentAmount = cmd.LeaveEncashmentAmount,
            GratuityAmount = cmd.GratuityAmount,
            OtherEarnings = cmd.OtherEarnings,
            OutstandingLoan = cmd.OutstandingLoan,
            OtherDeductions = cmd.OtherDeductions,
            GrossPayable = gross,
            TotalDeductions = totalDed,
            NetPayable = net,
            Status = FinalSettlementStatus.Draft,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entity.Id, "Settlement draft created.");
    }

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}

// ───────────────────────────────────────────────────────────────────────────
//   Approve (Draft → Approved): marks Employee Inactive / Terminated
// ───────────────────────────────────────────────────────────────────────────
public sealed record ApproveFinalSettlementCommand(long Id) : IRequest<ApiResponse>;

internal sealed class ApproveFinalSettlementCommandHandler
    : IRequestHandler<ApproveFinalSettlementCommand, ApiResponse>
{
    private readonly IRepository<FinalSettlement, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IRepository<Domain.Entities.CostCenter> _ccRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IJournalPostingService _journal;

    public ApproveFinalSettlementCommandHandler(
        IRepository<FinalSettlement, long> repo,
        IRepository<Domain.Entities.Employee> empRepo,
        IRepository<Domain.Entities.CostCenter> ccRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IJournalPostingService journal)
    {
        _repo = repo; _empRepo = empRepo; _ccRepo = ccRepo; _uow = uow;
        _currentUser = currentUser; _journal = journal;
    }

    public async Task<ApiResponse> Handle(ApproveFinalSettlementCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.Id, ct);
        if (s is null) return ApiResponse.Fail("Settlement not found.");
        if (s.Status != FinalSettlementStatus.Draft)
            return ApiResponse.Fail($"Cannot approve a {s.Status} settlement.");

        var emp = await _empRepo.GetByIdAsync(s.EmployeeId, ct);
        if (emp is null) return ApiResponse.Fail("Employee not found.");

        s.Status = FinalSettlementStatus.Approved;
        s.ApprovedAt = DateTimeOffset.UtcNow;
        s.ApprovedByUser = _currentUser.UserName ?? "system";

        emp.IsActive = false;
        emp.Status = s.Reason switch
        {
            SettlementReason.Termination => EmployeeStatus.Terminated,
            _ => EmployeeStatus.Inactive
        };

        // Phase A5 — book the settlement liability (final dues expensed, loan receivable recovered):
        //   Dr 5200 Salary Expense   = ProratedSalary + LeaveEncashment + OtherEarnings
        //   Dr 5210 Staff Cost       = Gratuity
        //     Cr 1190 Employee Loans = OutstandingLoan (recovers receivable)
        //     Cr 5200 (contra)       = OtherDeductions (recovery against staff cost)
        //     Cr 2130 Salary Payable = NetPayable   (Dr instead when the employee owes, i.e. net < 0)
        int? deptCc = await PayrollAccrual.DeptCostCenterAsync(_ccRepo, emp.DepartmentId, ct);
        var dims = deptCc is null ? (Dimensions?)null : new Dimensions(CostCenterId: deptCc);

        var earnings = s.ProratedSalary + s.LeaveEncashmentAmount + s.OtherEarnings;
        var lines = new List<JournalPostingLine>();
        if (earnings > 0m) lines.Add(new(LedgerAccounts.SalaryExpense, earnings, 0m, dims));
        if (s.GratuityAmount > 0m) lines.Add(new(LedgerAccounts.EmployerPfGratuity, s.GratuityAmount, 0m, dims));
        if (s.OutstandingLoan > 0m) lines.Add(new(LedgerAccounts.EmployeeLoans, 0m, s.OutstandingLoan));
        if (s.OtherDeductions > 0m) lines.Add(new(LedgerAccounts.SalaryExpense, 0m, s.OtherDeductions, dims));
        if (s.NetPayable > 0m) lines.Add(new(LedgerAccounts.SalaryPayable, 0m, s.NetPayable));
        else if (s.NetPayable < 0m) lines.Add(new(LedgerAccounts.SalaryPayable, -s.NetPayable, 0m));

        if (lines.Count > 0)
            await _journal.PostAsync(
                s.SettlementDate,
                $"Final settlement {s.Code} — {emp.FullName} ({emp.Code})",
                "FinalSettlement", s.Id, s.Code, lines, ct);

        _repo.Update(s);
        _empRepo.Update(emp);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Settlement approved. Employee marked inactive.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Mark Paid (Approved → Paid): closes employee's active loans
// ───────────────────────────────────────────────────────────────────────────
public sealed record MarkFinalSettlementPaidCommand(long Id, string PaymentMethod, string? PaymentReference)
    : IRequest<ApiResponse>;

public sealed class MarkFinalSettlementPaidCommandValidator : AbstractValidator<MarkFinalSettlementPaidCommand>
{
    public MarkFinalSettlementPaidCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(30);
        RuleFor(x => x.PaymentReference).MaximumLength(100);
    }
}

internal sealed class MarkFinalSettlementPaidCommandHandler
    : IRequestHandler<MarkFinalSettlementPaidCommand, ApiResponse>
{
    private readonly IRepository<FinalSettlement, long> _repo;
    private readonly IRepository<EmployeeLoan, long> _loanRepo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;

    public MarkFinalSettlementPaidCommandHandler(
        IRepository<FinalSettlement, long> repo,
        IRepository<EmployeeLoan, long> loanRepo,
        IUnitOfWork uow,
        IJournalPostingService journal)
    {
        _repo = repo; _loanRepo = loanRepo; _uow = uow; _journal = journal;
    }

    public async Task<ApiResponse> Handle(MarkFinalSettlementPaidCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.Id, ct);
        if (s is null) return ApiResponse.Fail("Settlement not found.");
        if (s.Status != FinalSettlementStatus.Approved)
            return ApiResponse.Fail($"Cannot mark paid: settlement is {s.Status}.");

        s.Status = FinalSettlementStatus.Paid;
        s.PaidAt = DateTimeOffset.UtcNow;
        s.PaymentMethod = cmd.PaymentMethod.Trim();
        s.PaymentReference = string.IsNullOrWhiteSpace(cmd.PaymentReference) ? null : cmd.PaymentReference.Trim();
        _repo.Update(s);

        // Phase A5 — settle the net payable raised at Approve: Dr Salary Payable / Cr Cash|Bank
        // (reversed when the employee owed a net, i.e. NetPayable < 0).
        if (s.NetPayable != 0m)
        {
            var cashAccount = s.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)
                ? LedgerAccounts.Cash : LedgerAccounts.Bank;
            var abs = Math.Abs(s.NetPayable);
            var lines = s.NetPayable > 0m
                ? new[] { new JournalPostingLine(LedgerAccounts.SalaryPayable, abs, 0m), new JournalPostingLine(cashAccount, 0m, abs) }
                : new[] { new JournalPostingLine(cashAccount, abs, 0m), new JournalPostingLine(LedgerAccounts.SalaryPayable, 0m, abs) };
            await _journal.PostAsync(
                s.SettlementDate, $"Final settlement {s.Code} paid", "FinalSettlement", s.Id, s.Code, lines, ct);
        }

        // Close any active loans for this employee — the receivable was already recovered in the
        // Approve accrual (Cr 1190 for the outstanding snapshot); here we just close the records.
        var activeLoans = await _loanRepo.Query()
            .Where(l => l.EmployeeId == s.EmployeeId && l.Status == EmployeeLoanStatus.Active)
            .ToListAsync(ct);
        foreach (var loan in activeLoans)
        {
            loan.OutstandingPrincipal = 0m;
            loan.Status = EmployeeLoanStatus.Closed;
            _loanRepo.Update(loan);
        }

        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Settlement paid. Outstanding loans closed.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Cancel (Draft / Approved → Cancelled; reactivate employee if was Approved)
// ───────────────────────────────────────────────────────────────────────────
public sealed record CancelFinalSettlementCommand(long Id) : IRequest<ApiResponse>;

internal sealed class CancelFinalSettlementCommandHandler
    : IRequestHandler<CancelFinalSettlementCommand, ApiResponse>
{
    private readonly IRepository<FinalSettlement, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IUnitOfWork _uow;

    public CancelFinalSettlementCommandHandler(
        IRepository<FinalSettlement, long> repo,
        IRepository<Domain.Entities.Employee> empRepo,
        IUnitOfWork uow)
    {
        _repo = repo; _empRepo = empRepo; _uow = uow;
    }

    public async Task<ApiResponse> Handle(CancelFinalSettlementCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.Id, ct);
        if (s is null) return ApiResponse.Fail("Settlement not found.");
        if (s.Status == FinalSettlementStatus.Paid)
            return ApiResponse.Fail("Cannot cancel a Paid settlement.");
        if (s.Status == FinalSettlementStatus.Cancelled)
            return ApiResponse.Fail("Settlement is already cancelled.");

        var wasApproved = s.Status == FinalSettlementStatus.Approved;
        s.Status = FinalSettlementStatus.Cancelled;
        _repo.Update(s);

        if (wasApproved)
        {
            var emp = await _empRepo.GetByIdAsync(s.EmployeeId, ct);
            if (emp is not null)
            {
                emp.IsActive = true;
                emp.Status = EmployeeStatus.Active;
                _empRepo.Update(emp);
            }
        }

        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok(wasApproved
            ? "Settlement cancelled. Employee reactivated."
            : "Draft settlement cancelled.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Bank-advice CSV — for a payroll month, list all Paid payslips with bank info
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetBankAdviceQuery(int Year, int Month) : IRequest<ApiResponse<IReadOnlyList<BankAdviceRowDto>>>;

public sealed class GetBankAdviceQueryValidator : AbstractValidator<GetBankAdviceQuery>
{
    public GetBankAdviceQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

internal sealed class GetBankAdviceQueryHandler
    : IRequestHandler<GetBankAdviceQuery, ApiResponse<IReadOnlyList<BankAdviceRowDto>>>
{
    private readonly IRepository<Payslip, long> _payslipRepo;

    public GetBankAdviceQueryHandler(IRepository<Payslip, long> payslipRepo) => _payslipRepo = payslipRepo;

    public async Task<ApiResponse<IReadOnlyList<BankAdviceRowDto>>> Handle(GetBankAdviceQuery q, CancellationToken ct)
    {
        var rows = await _payslipRepo.Query()
            .Where(p => p.Year == q.Year && p.Month == q.Month)
            .OrderBy(p => p.Employee.Code)
            .Select(p => new BankAdviceRowDto(
                p.Employee.Code,
                p.Employee.FullName,
                p.Employee.BankAccount != null ? p.Employee.BankAccount.BankName : null,
                p.Employee.BankAccount != null ? p.Employee.BankAccount.BranchName : null,
                p.Employee.BankAccount != null ? p.Employee.BankAccount.AccountNumber : null,
                p.Employee.BankAccount != null ? p.Employee.BankAccount.RoutingNumber : null,
                p.NetPay))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<BankAdviceRowDto>>.Ok(rows);
    }
}
