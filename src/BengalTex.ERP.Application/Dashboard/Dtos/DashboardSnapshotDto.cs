namespace BengalTex.ERP.Application.Dashboard.Dtos;

// ─── Hero strip (visible to all who can see Dashboard at all) ───────────────
public record HeroKpiDto(
    decimal CashAndBankBalance,        // Σ (Dr − Cr) on Cash (1110) + Bank (1120) ledger accounts, posted
    decimal ThisMonthRevenue,          // Σ TotalAmount (BDT) on non-Draft/non-Cancelled CustomerInvoices this calendar month
    decimal TotalStockValue,           // Σ qty × WAC across RM + Product
    int ActiveOrdersCount,             // Open + InProgress sales orders + purchase orders
    decimal OutstandingArAmount,
    decimal OutstandingApAmount,
    // ── Redesign additions (additive) ──
    decimal RawMaterialStockValue = 0m,
    decimal FinishedGoodsStockValue = 0m,
    decimal OverdueArAmount = 0m,       // Σ outstanding on Issued/PartiallyPaid invoices past their DueDate
    int OverdueArCount = 0);

// ─── Today KPIs (financial — gated: Owner / Accounts / Sales) ───────────────
public record TodayKpisDto(
    decimal TodaySales, decimal PrevDaySales,
    decimal TodayPurchase, decimal PrevDayPurchase,
    decimal TodayExpenses, decimal PrevDayExpenses,
    IReadOnlyList<decimal> SalesSpark,      // last 7 days daily totals (oldest → today)
    IReadOnlyList<decimal> PurchaseSpark,
    IReadOnlyList<decimal> ExpenseSpark);

// ─── Expense breakdown (this month, by expense account) — gated: Owner / Accounts ──
public record ExpenseBreakdownItemDto(string Name, decimal Amount);

// ─── Low-stock alert — gated: Production / Owner ───────────────────────────
public record LowStockItemDto(
    string ItemName, decimal Available, decimal ReorderLevel, string Unit, string Status);  // Critical | Warning | Normal

// ─── Production overview + recent orders (in the Production section) ────────
public record ProductionOverviewDto(decimal Target, decimal Produced, decimal AchievementPct);

public record RecentProductionOrderDto(
    string Code, string ProductName, string? StyleName, decimal Quantity, string Status, decimal ProgressPct);

// ─── Attendance breakdown (in the HR section) ──────────────────────────────
public record AttendanceBreakdownDto(
    int TotalActive, int Present, int Absent, int Late, int OnLeave, decimal AttendancePct);

// ─── Upcoming salary (HR section; salary date falls back to month-end) ─────
public record UpcomingSalaryDto(
    int Year, int Month, string MonthLabel, int EligibleEmployees, decimal EstimatedAmount,
    DateOnly SalaryDate, int RemainingDays, string Status);   // Upcoming | DueSoon | Due | Overdue

// ─── Per-section snapshots ─────────────────────────────────────────────────
public record SalesSectionDto(
    int PendingQuotations,
    int OpenSalesOrders,
    int ThisMonthInvoiceCount,
    decimal ThisMonthRevenue);

public record ProcurementSectionDto(
    int OpenPurchaseOrders,
    int PendingGrns,                   // Approved POs with received < ordered
    int ThisMonthPaymentCount,
    decimal ThisMonthPaymentAmount);

public record ProductionSectionDto(
    int ActiveProductionOrders,
    int OpenJobCards,
    int InProgressJobCards,
    decimal ThisMonthWastageCost,
    // Phase 7 — manufacturing KPIs
    int CompletedThisMonth = 0,
    int DelayedProductions = 0,        // InProgress past planned end date
    int QcHeldProductions = 0,         // completed + QC-held, not yet released
    int MachinesUnderMaintenance = 0,
    ProductionOverviewDto? Overview = null,
    IReadOnlyList<RecentProductionOrderDto>? RecentOrders = null);

public record HrSectionDto(
    int ActiveEmployees,
    int PresentToday,
    int PendingLeaveApplications,
    int ActiveLoans,
    AttendanceBreakdownDto? Attendance = null,
    UpcomingSalaryDto? UpcomingSalary = null);

public record AccountingSectionDto(
    decimal OutstandingArAmount,
    int OutstandingArInvoices,
    decimal OutstandingApAmount,
    int OutstandingApInvoices,
    int UnreconciledStatements,
    int ThisMonthJournalEntries);

public record ComplianceSectionDto(
    int CertificatesExpiringSoon,      // ≤ 60d
    int CertificatesExpired,
    int UpcomingAudits,                // next 30d, Scheduled
    int OpenCapFindings,
    int OverdueCapFindings);

// ─── Revenue trend (last 12 months, hero-level) ────────────────────────────
public record MonthlyTrendPointDto(
    int Year,
    int Month,
    string Label,                      // "Jun" — short month abbreviation
    decimal Revenue);                  // Σ TotalAmount (BDT) for non-Draft/Cancelled invoices that month

// ─── Needs-attention lists ─────────────────────────────────────────────────
public record NeedsAttentionItemDto(
    string Type,                       // "ExpiringCert" | "OverdueCap" | "PendingLeave" | "UnreconciledStmt" | "OverdueInvoice"
    string Title,
    string Detail,
    string? Reference,                 // Code or short identifier
    DateOnly? Date,
    string? Severity);                 // "Critical" | "Warning" | "Info"

public record DashboardSnapshotDto(
    DateTimeOffset GeneratedAt,
    HeroKpiDto Hero,
    IReadOnlyList<MonthlyTrendPointDto> RevenueTrend,   // last 12 months, oldest → newest
    SalesSectionDto? Sales,            // null = caller has no Sales view permission
    ProcurementSectionDto? Procurement,
    ProductionSectionDto? Production,
    HrSectionDto? Hr,
    AccountingSectionDto? Accounting,
    ComplianceSectionDto? Compliance,
    IReadOnlyList<NeedsAttentionItemDto> NeedsAttention,
    // ── Redesign additions (additive) ──
    TodayKpisDto? TodayKpis = null,
    IReadOnlyList<ExpenseBreakdownItemDto>? ExpenseBreakdown = null,
    IReadOnlyList<LowStockItemDto>? LowStock = null);
