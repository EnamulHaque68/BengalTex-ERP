using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Dashboard.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Dashboard.Queries;

/// <summary>
/// Single-round-trip snapshot of the entire factory: hero KPIs + per-section
/// counts + top "needs attention" items. Sections are gated by the caller's
/// permissions — a user without Dashboard.ViewSales gets <c>Sales=null</c>.
/// </summary>
public sealed record GetDashboardSnapshotQuery : IRequest<ApiResponse<DashboardSnapshotDto>>;

internal sealed class GetDashboardSnapshotQueryHandler
    : IRequestHandler<GetDashboardSnapshotQuery, ApiResponse<DashboardSnapshotDto>>
{
    private readonly IRepository<JournalEntryLine, long> _jLineRepo;
    private readonly IRepository<Account> _acctRepo;
    private readonly IRepository<StockOnHand> _stockRepo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _arRepo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _apRepo;
    private readonly IRepository<Domain.Entities.Quotation, long> _quotRepo;
    private readonly IRepository<Domain.Entities.SalesOrder, long> _soRepo;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _grnRepo;
    private readonly IRepository<Domain.Entities.Payment, long> _payRepo;
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _prodRepo;
    private readonly IRepository<JobCard, long> _jcRepo;
    private readonly IRepository<WastageEntry, long> _wasteRepo;
    private readonly IRepository<Domain.Entities.MachineMaintenance, long> _maintRepo;
    private readonly IRepository<Domain.Entities.Employee> _empRepo;
    private readonly IRepository<AttendanceRecord, long> _attRepo;
    private readonly IRepository<LeaveApplication, long> _leaveRepo;
    private readonly IRepository<EmployeeLoan, long> _loanRepo;
    private readonly IRepository<BankStatement, long> _stmtRepo;
    private readonly IRepository<JournalEntry, long> _jRepo;
    private readonly IRepository<ComplianceCertificate> _certRepo;
    private readonly IRepository<ComplianceAudit, long> _auditRepo;
    private readonly IRepository<AuditFinding, long> _findingRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetDashboardSnapshotQueryHandler(
        IRepository<JournalEntryLine, long> jLineRepo,
        IRepository<Account> acctRepo,
        IRepository<StockOnHand> stockRepo,
        IRepository<Domain.Entities.CustomerInvoice, long> arRepo,
        IRepository<Domain.Entities.SupplierInvoice, long> apRepo,
        IRepository<Domain.Entities.Quotation, long> quotRepo,
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IRepository<Domain.Entities.GoodsReceiptNote, long> grnRepo,
        IRepository<Domain.Entities.Payment, long> payRepo,
        IRepository<Domain.Entities.ProductionOrder, long> prodRepo,
        IRepository<JobCard, long> jcRepo,
        IRepository<WastageEntry, long> wasteRepo,
        IRepository<Domain.Entities.MachineMaintenance, long> maintRepo,
        IRepository<Domain.Entities.Employee> empRepo,
        IRepository<AttendanceRecord, long> attRepo,
        IRepository<LeaveApplication, long> leaveRepo,
        IRepository<EmployeeLoan, long> loanRepo,
        IRepository<BankStatement, long> stmtRepo,
        IRepository<JournalEntry, long> jRepo,
        IRepository<ComplianceCertificate> certRepo,
        IRepository<ComplianceAudit, long> auditRepo,
        IRepository<AuditFinding, long> findingRepo,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _jLineRepo = jLineRepo; _acctRepo = acctRepo; _stockRepo = stockRepo;
        _arRepo = arRepo; _apRepo = apRepo;
        _quotRepo = quotRepo; _soRepo = soRepo; _poRepo = poRepo;
        _grnRepo = grnRepo; _payRepo = payRepo;
        _prodRepo = prodRepo; _jcRepo = jcRepo; _wasteRepo = wasteRepo; _maintRepo = maintRepo;
        _empRepo = empRepo; _attRepo = attRepo; _leaveRepo = leaveRepo; _loanRepo = loanRepo;
        _stmtRepo = stmtRepo; _jRepo = jRepo;
        _certRepo = certRepo; _auditRepo = auditRepo; _findingRepo = findingRepo;
        _currentUser = currentUser; _clock = clock;
    }

    public async Task<ApiResponse<DashboardSnapshotDto>> Handle(GetDashboardSnapshotQuery request, CancellationToken ct)
    {
        var today = _clock.Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var soonCutoff = today.AddDays(60);
        var auditCutoff = today.AddDays(30);

        // ── HERO STRIP (visible to all) ────────────────────────────────────
        var cashAccountIds = await _acctRepo.Query()
            .Where(a => a.Code == LedgerAccounts.Cash || a.Code == LedgerAccounts.Bank)
            .Select(a => a.Id).ToListAsync(ct);
        var cashAndBank = cashAccountIds.Count == 0 ? 0m : await _jLineRepo.Query()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && cashAccountIds.Contains(l.AccountId))
            .GroupBy(_ => 1)
            .Select(g => g.Sum(x => x.Debit - x.Credit))
            .FirstOrDefaultAsync(ct);

        var thisMonthRevenue = await _arRepo.Query()
            .Where(i => i.InvoiceDate >= monthStart && i.InvoiceDate <= monthEnd
                     && i.Status != CustomerInvoiceStatus.Draft
                     && i.Status != CustomerInvoiceStatus.Cancelled)
            .SumAsync(i => (decimal?)(i.TotalAmount * i.ExchangeRate), ct) ?? 0m;

        var rmStockValue = await _stockRepo.Query()
            .Where(s => s.RawMaterialId != null)
            .SumAsync(s => (decimal?)(s.Quantity * s.RawMaterial!.WeightedAverageCost), ct) ?? 0m;
        var productStockValue = await _stockRepo.Query()
            .Where(s => s.ProductId != null)
            .SumAsync(s => (decimal?)(s.Quantity * s.Product!.WeightedAverageCost), ct) ?? 0m;
        var totalStockValue = rmStockValue + productStockValue;

        var activeSoCount = await _soRepo.Query()
            .CountAsync(o => o.Status == SalesOrderStatus.Confirmed || o.Status == SalesOrderStatus.PartiallyDispatched, ct);
        var activePoCount = await _poRepo.Query()
            .CountAsync(o => o.Status == PurchaseOrderStatus.Approved || o.Status == PurchaseOrderStatus.PartiallyReceived, ct);

        var arOutstanding = await _arRepo.Query()
            .Where(i => (i.Status == CustomerInvoiceStatus.Issued || i.Status == CustomerInvoiceStatus.PartiallyPaid)
                     && i.TotalAmount - i.AmountPaid > 0m)
            .SumAsync(i => (decimal?)((i.TotalAmount - i.AmountPaid) * i.ExchangeRate), ct) ?? 0m;
        var apOutstanding = await _apRepo.Query()
            .Where(i => (i.Status == SupplierInvoiceStatus.Approved || i.Status == SupplierInvoiceStatus.PartiallyPaid)
                     && i.TotalAmount - i.AmountPaid > 0m)
            .SumAsync(i => (decimal?)((i.TotalAmount - i.AmountPaid) * i.ExchangeRate), ct) ?? 0m;

        var hero = new HeroKpiDto(cashAndBank, thisMonthRevenue, totalStockValue,
            activeSoCount + activePoCount, arOutstanding, apOutstanding);

        // ── REVENUE TREND (last 12 months, visible to all) ─────────────────
        var trendStart = monthStart.AddMonths(-11);
        var trendRaw = await _arRepo.Query()
            .Where(i => i.InvoiceDate >= trendStart && i.InvoiceDate <= monthEnd
                     && i.Status != CustomerInvoiceStatus.Draft
                     && i.Status != CustomerInvoiceStatus.Cancelled)
            .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(x => x.TotalAmount * x.ExchangeRate) })
            .ToListAsync(ct);
        var revenueTrend = new List<MonthlyTrendPointDto>(12);
        for (var k = 0; k < 12; k++)
        {
            var m = trendStart.AddMonths(k);
            var hit = trendRaw.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month);
            revenueTrend.Add(new MonthlyTrendPointDto(
                m.Year, m.Month, m.ToString("MMM"), hit?.Revenue ?? 0m));
        }

        // ── SECTIONS (gated by permission) ─────────────────────────────────
        SalesSectionDto? sales = null;
        if (_currentUser.HasPermission(Permissions.Dashboard.ViewSales) || _currentUser.HasPermission(Permissions.Dashboard.ViewOwner))
        {
            var pendingQuot = await _quotRepo.Query()
                .CountAsync(q => q.Status == QuotationStatus.Draft || q.Status == QuotationStatus.Sent, ct);
            var openSo = await _soRepo.Query()
                .CountAsync(o => o.Status == SalesOrderStatus.Confirmed || o.Status == SalesOrderStatus.PartiallyDispatched, ct);
            var thisMonthInvCount = await _arRepo.Query()
                .CountAsync(i => i.InvoiceDate >= monthStart && i.InvoiceDate <= monthEnd
                              && i.Status != CustomerInvoiceStatus.Draft
                              && i.Status != CustomerInvoiceStatus.Cancelled, ct);
            sales = new SalesSectionDto(pendingQuot, openSo, thisMonthInvCount, thisMonthRevenue);
        }

        ProcurementSectionDto? procurement = null;
        if (_currentUser.HasPermission(Permissions.Dashboard.ViewProduction) || _currentUser.HasPermission(Permissions.Dashboard.ViewOwner))
        {
            var openPo = await _poRepo.Query()
                .CountAsync(o => o.Status == PurchaseOrderStatus.Approved || o.Status == PurchaseOrderStatus.PartiallyReceived, ct);
            var pendingGrn = await _poRepo.Query()
                .CountAsync(o => o.Status == PurchaseOrderStatus.Approved || o.Status == PurchaseOrderStatus.PartiallyReceived, ct);
            var thisMonthPay = await _payRepo.Query()
                .Where(p => p.PaymentDate >= monthStart && p.PaymentDate <= monthEnd)
                .GroupBy(_ => 1)
                .Select(g => new { Count = g.Count(), Sum = g.Sum(p => p.Amount * p.SupplierInvoice.ExchangeRate) })
                .FirstOrDefaultAsync(ct);
            procurement = new ProcurementSectionDto(openPo, pendingGrn,
                thisMonthPay?.Count ?? 0, thisMonthPay?.Sum ?? 0m);
        }

        ProductionSectionDto? production = null;
        if (_currentUser.HasPermission(Permissions.Dashboard.ViewProduction) || _currentUser.HasPermission(Permissions.Dashboard.ViewOwner))
        {
            var activeProd = await _prodRepo.Query()
                .CountAsync(p => p.Status == ProductionOrderStatus.InProgress, ct);
            var openJc = await _jcRepo.Query().CountAsync(j => j.Status == JobCardStatus.Open, ct);
            var inProgJc = await _jcRepo.Query().CountAsync(j => j.Status == JobCardStatus.InProgress, ct);
            var monthlyWaste = await _wasteRepo.Query()
                .Where(w => w.WastageDate >= monthStart && w.WastageDate <= monthEnd)
                .SumAsync(w => (decimal?)w.TotalCost, ct) ?? 0m;
            var completedThisMonth = await _prodRepo.Query()
                .CountAsync(p => p.Status == ProductionOrderStatus.Completed
                              && p.ActualEndDate != null && p.ActualEndDate >= monthStart && p.ActualEndDate <= monthEnd, ct);
            var delayedProd = await _prodRepo.Query()
                .CountAsync(p => p.Status == ProductionOrderStatus.InProgress
                              && p.PlannedEndDate != null && p.PlannedEndDate < today, ct);
            var qcHeldProd = await _prodRepo.Query()
                .CountAsync(p => p.RequiresQc && p.Status == ProductionOrderStatus.Completed && p.QcReleasedAt == null, ct);
            var machinesUnderMaint = await _maintRepo.Query()
                .CountAsync(m => m.Status == MaintenanceStatus.InProgress, ct);
            production = new ProductionSectionDto(activeProd, openJc, inProgJc, monthlyWaste,
                completedThisMonth, delayedProd, qcHeldProd, machinesUnderMaint);
        }

        HrSectionDto? hr = null;
        if (_currentUser.HasPermission(Permissions.Dashboard.ViewHr) || _currentUser.HasPermission(Permissions.Dashboard.ViewOwner))
        {
            var active = await _empRepo.Query()
                .CountAsync(e => e.IsActive && e.Status == EmployeeStatus.Active, ct);
            var presentToday = await _attRepo.Query()
                .CountAsync(a => a.AttendanceDate == today
                              && (a.Status == AttendanceStatus.Present
                                  || a.Status == AttendanceStatus.Late
                                  || a.Status == AttendanceStatus.HalfDay), ct);
            var pendingLeave = await _leaveRepo.Query()
                .CountAsync(l => l.Status == LeaveApplicationStatus.Pending, ct);
            var activeLoans = await _loanRepo.Query()
                .CountAsync(l => l.Status == EmployeeLoanStatus.Active, ct);
            hr = new HrSectionDto(active, presentToday, pendingLeave, activeLoans);
        }

        AccountingSectionDto? accounting = null;
        if (_currentUser.HasPermission(Permissions.Dashboard.ViewAccounts) || _currentUser.HasPermission(Permissions.Dashboard.ViewOwner))
        {
            var arOutstandingCount = await _arRepo.Query()
                .CountAsync(i => (i.Status == CustomerInvoiceStatus.Issued || i.Status == CustomerInvoiceStatus.PartiallyPaid)
                              && i.TotalAmount - i.AmountPaid > 0m, ct);
            var apOutstandingCount = await _apRepo.Query()
                .CountAsync(i => (i.Status == SupplierInvoiceStatus.Approved || i.Status == SupplierInvoiceStatus.PartiallyPaid)
                              && i.TotalAmount - i.AmountPaid > 0m, ct);
            var unreconciled = await _stmtRepo.Query().CountAsync(s => !s.IsReconciled, ct);
            var monthlyJe = await _jRepo.Query()
                .CountAsync(j => j.EntryDate >= monthStart && j.EntryDate <= monthEnd
                              && j.Status == JournalEntryStatus.Posted, ct);
            accounting = new AccountingSectionDto(
                arOutstanding, arOutstandingCount,
                apOutstanding, apOutstandingCount,
                unreconciled, monthlyJe);
        }

        ComplianceSectionDto? compliance = null;
        if (_currentUser.HasPermission(Permissions.Compliance.View))
        {
            var allCerts = await _certRepo.Query()
                .Where(c => c.IsActive)
                .Select(c => c.ExpiryDate).ToListAsync(ct);
            var expSoon = allCerts.Count(d => d >= today && d <= soonCutoff);
            var expired = allCerts.Count(d => d < today);
            var upcomingAudits = await _auditRepo.Query()
                .CountAsync(a => a.Status == ComplianceAuditStatus.Scheduled
                              && a.ScheduledDate >= today && a.ScheduledDate <= auditCutoff, ct);
            var openFindings = await _findingRepo.Query()
                .CountAsync(f => f.Status == AuditFindingStatus.Open || f.Status == AuditFindingStatus.InProgress, ct);
            var overdueFindings = await _findingRepo.Query()
                .CountAsync(f => (f.Status == AuditFindingStatus.Open || f.Status == AuditFindingStatus.InProgress)
                              && f.DueDate.HasValue && f.DueDate.Value < today, ct);
            compliance = new ComplianceSectionDto(expSoon, expired, upcomingAudits, openFindings, overdueFindings);
        }

        // ── NEEDS-ATTENTION list (top 10 across categories) ────────────────
        var needs = new List<NeedsAttentionItemDto>();

        if (compliance is not null)
        {
            var expiringCerts = await _certRepo.Query()
                .Where(c => c.IsActive && c.ExpiryDate <= soonCutoff)
                .OrderBy(c => c.ExpiryDate)
                .Take(5)
                .Select(c => new { c.Name, c.CertificateType, c.ExpiryDate })
                .ToListAsync(ct);
            foreach (var c in expiringCerts)
            {
                var days = c.ExpiryDate.DayNumber - today.DayNumber;
                needs.Add(new NeedsAttentionItemDto(
                    "ExpiringCert", c.Name, c.CertificateType.ToString(),
                    c.ExpiryDate.ToString("yyyy-MM-dd"), c.ExpiryDate,
                    days < 0 ? "Critical" : days <= 30 ? "Warning" : "Info"));
            }

            var overdueCap = await _findingRepo.Query()
                .Where(f => (f.Status == AuditFindingStatus.Open || f.Status == AuditFindingStatus.InProgress)
                         && f.DueDate.HasValue && f.DueDate.Value < today)
                .OrderBy(f => f.DueDate)
                .Take(5)
                .Select(f => new { f.FindingDescription, f.Severity, f.DueDate, AuditCode = f.ComplianceAudit.Code })
                .ToListAsync(ct);
            foreach (var f in overdueCap)
                needs.Add(new NeedsAttentionItemDto(
                    "OverdueCap", "CAP item overdue", f.FindingDescription,
                    f.AuditCode, f.DueDate, "Critical"));
        }

        if (hr is not null && hr.PendingLeaveApplications > 0)
        {
            var leaves = await _leaveRepo.Query()
                .Where(l => l.Status == LeaveApplicationStatus.Pending)
                .OrderBy(l => l.CreatedAt)
                .Take(5)
                .Select(l => new { l.Code, EmpName = l.Employee.FullName, l.FromDate, l.ToDate, l.TotalDays })
                .ToListAsync(ct);
            foreach (var l in leaves)
                needs.Add(new NeedsAttentionItemDto(
                    "PendingLeave", $"Leave {l.Code} pending",
                    $"{l.EmpName} — {l.TotalDays} day(s)", l.Code, l.FromDate, "Info"));
        }

        if (accounting is not null && accounting.UnreconciledStatements > 0)
        {
            var unrec = await _stmtRepo.Query()
                .Where(s => !s.IsReconciled)
                .OrderBy(s => s.StatementDate)
                .Take(3)
                .Select(s => new { s.Code, BankName = s.BankAccount.AccountName, s.StatementDate })
                .ToListAsync(ct);
            foreach (var s in unrec)
                needs.Add(new NeedsAttentionItemDto(
                    "UnreconciledStmt", $"Statement {s.Code} unreconciled",
                    s.BankName, s.Code, s.StatementDate, "Warning"));
        }

        if (production is not null)
        {
            if (production.DelayedProductions > 0)
            {
                var delayed = await _prodRepo.Query()
                    .Where(p => p.Status == ProductionOrderStatus.InProgress
                             && p.PlannedEndDate != null && p.PlannedEndDate < today)
                    .OrderBy(p => p.PlannedEndDate).Take(5)
                    .Select(p => new { p.Code, ProductName = p.Product.Name, p.PlannedEndDate })
                    .ToListAsync(ct);
                foreach (var d in delayed)
                    needs.Add(new NeedsAttentionItemDto(
                        "DelayedProduction", $"Production {d.Code} overdue", d.ProductName,
                        d.Code, d.PlannedEndDate, "Warning"));
            }
            if (production.MachinesUnderMaintenance > 0)
            {
                var machines = await _maintRepo.Query()
                    .Where(m => m.Status == MaintenanceStatus.InProgress)
                    .OrderBy(m => m.ScheduledDate).Take(3)
                    .Select(m => new { m.Code, MachineName = m.Machine.Name })
                    .ToListAsync(ct);
                foreach (var m in machines)
                    needs.Add(new NeedsAttentionItemDto(
                        "MachineMaintenance", "Machine under maintenance", m.MachineName,
                        m.Code, null, "Warning"));
            }
        }

        return ApiResponse<DashboardSnapshotDto>.Ok(new DashboardSnapshotDto(
            _clock.UtcNow, hero, revenueTrend, sales, procurement, production, hr,
            accounting, compliance, needs));
    }
}
