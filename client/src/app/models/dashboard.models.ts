// ─── Dashboard ────────────────────────────────────────────────────────────

export interface HeroKpiDto {
  cashAndBankBalance: number;
  thisMonthRevenue: number;
  totalStockValue: number;
  activeOrdersCount: number;
  outstandingArAmount: number;
  outstandingApAmount: number;
  rawMaterialStockValue: number;
  finishedGoodsStockValue: number;
  overdueArAmount: number;
  overdueArCount: number;
}

export interface TodayKpisDto {
  todaySales: number; prevDaySales: number;
  todayPurchase: number; prevDayPurchase: number;
  todayExpenses: number; prevDayExpenses: number;
  salesSpark: number[]; purchaseSpark: number[]; expenseSpark: number[];
}

export interface ExpenseBreakdownItemDto { name: string; amount: number; }

export interface LowStockItemDto {
  itemName: string; available: number; reorderLevel: number; unit: string; status: string;
}

export interface ProductionOverviewDto { target: number; produced: number; achievementPct: number; }

export interface RecentProductionOrderDto {
  code: string; productName: string; styleName: string | null; quantity: number; status: string; progressPct: number;
}

export interface AttendanceBreakdownDto {
  totalActive: number; present: number; absent: number; late: number; onLeave: number; attendancePct: number;
}

export interface UpcomingSalaryDto {
  year: number; month: number; monthLabel: string; eligibleEmployees: number; estimatedAmount: number;
  salaryDate: string; remainingDays: number; status: string;
}

export interface SalesSectionDto {
  pendingQuotations: number;
  openSalesOrders: number;
  thisMonthInvoiceCount: number;
  thisMonthRevenue: number;
}

export interface ProcurementSectionDto {
  openPurchaseOrders: number;
  pendingGrns: number;
  thisMonthPaymentCount: number;
  thisMonthPaymentAmount: number;
}

export interface ProductionSectionDto {
  activeProductionOrders: number;
  openJobCards: number;
  inProgressJobCards: number;
  thisMonthWastageCost: number;
  // Phase 7
  completedThisMonth?: number;
  delayedProductions?: number;
  qcHeldProductions?: number;
  machinesUnderMaintenance?: number;
  overview?: ProductionOverviewDto | null;
  recentOrders?: RecentProductionOrderDto[] | null;
}

export interface HrSectionDto {
  activeEmployees: number;
  presentToday: number;
  pendingLeaveApplications: number;
  activeLoans: number;
  attendance?: AttendanceBreakdownDto | null;
  upcomingSalary?: UpcomingSalaryDto | null;
}

export interface AccountingSectionDto {
  outstandingArAmount: number;
  outstandingArInvoices: number;
  outstandingApAmount: number;
  outstandingApInvoices: number;
  unreconciledStatements: number;
  thisMonthJournalEntries: number;
}

export interface ComplianceSectionDto {
  certificatesExpiringSoon: number;
  certificatesExpired: number;
  upcomingAudits: number;
  openCapFindings: number;
  overdueCapFindings: number;
}

export interface MonthlyTrendPointDto {
  year: number;
  month: number;
  label: string;
  revenue: number;
}

export interface NeedsAttentionItemDto {
  type: 'ExpiringCert' | 'OverdueCap' | 'PendingLeave' | 'UnreconciledStmt' | 'OverdueInvoice';
  title: string;
  detail: string;
  reference: string | null;
  date: string | null;
  severity: 'Critical' | 'Warning' | 'Info' | null;
}

export interface DashboardSnapshotDto {
  generatedAt: string;
  hero: HeroKpiDto;
  revenueTrend: MonthlyTrendPointDto[];
  sales: SalesSectionDto | null;
  procurement: ProcurementSectionDto | null;
  production: ProductionSectionDto | null;
  hr: HrSectionDto | null;
  accounting: AccountingSectionDto | null;
  compliance: ComplianceSectionDto | null;
  needsAttention: NeedsAttentionItemDto[];
  todayKpis?: TodayKpisDto | null;
  expenseBreakdown?: ExpenseBreakdownItemDto[] | null;
  lowStock?: LowStockItemDto[] | null;
}
