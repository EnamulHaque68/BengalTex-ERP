namespace BengalTex.ERP.Application.Dashboard.Dtos;

// ─── Hero strip (visible to all who can see Dashboard at all) ───────────────
public record HeroKpiDto(
    decimal CashAndBankBalance,        // Σ (Dr − Cr) on Cash (1110) + Bank (1120) ledger accounts, posted
    decimal ThisMonthRevenue,          // Σ TotalAmount (BDT) on non-Draft/non-Cancelled CustomerInvoices this calendar month
    decimal TotalStockValue,           // Σ qty × WAC across RM + Product
    int ActiveOrdersCount,             // Open + InProgress sales orders + purchase orders
    decimal OutstandingArAmount,
    decimal OutstandingApAmount);

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
    decimal ThisMonthWastageCost);

public record HrSectionDto(
    int ActiveEmployees,
    int PresentToday,
    int PendingLeaveApplications,
    int ActiveLoans);

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
    IReadOnlyList<NeedsAttentionItemDto> NeedsAttention);
