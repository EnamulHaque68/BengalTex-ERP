// ─── Dashboard ────────────────────────────────────────────────────────────

export interface HeroKpiDto {
  cashAndBankBalance: number;
  thisMonthRevenue: number;
  totalStockValue: number;
  activeOrdersCount: number;
  outstandingArAmount: number;
  outstandingApAmount: number;
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
}

export interface HrSectionDto {
  activeEmployees: number;
  presentToday: number;
  pendingLeaveApplications: number;
  activeLoans: number;
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
  sales: SalesSectionDto | null;
  procurement: ProcurementSectionDto | null;
  production: ProductionSectionDto | null;
  hr: HrSectionDto | null;
  accounting: AccountingSectionDto | null;
  compliance: ComplianceSectionDto | null;
  needsAttention: NeedsAttentionItemDto[];
}
