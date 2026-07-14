// ─── Phase A8 — financial intelligence ─────────────────────────────────────

export interface FinancialKpisDto {
  asOfDate: string; fromDate: string; toDate: string;
  currentAssets: number; currentLiabilities: number; inventory: number; workingCapital: number;
  totalAssets: number; totalLiabilities: number; totalEquity: number;
  accountsReceivable: number; accountsPayable: number;
  revenue: number; cogs: number; grossProfit: number; netProfit: number;
  currentRatio: number; quickRatio: number; debtToEquity: number;
  grossMarginPct: number; netMarginPct: number; returnOnAssetsPct: number;
  inventoryTurnover: number; dso: number; dpo: number;
}

export interface AgingRowDto {
  party: string;
  bucket0_30: number; bucket31_60: number; bucket61_90: number; bucket90Plus: number; total: number;
}
export interface AgingReportDto {
  kind: string; rows: AgingRowDto[];
  total0_30: number; total31_60: number; total61_90: number; total90Plus: number; grandTotal: number;
}
export interface ArApAgingDto { asOfDate: string; receivables: AgingReportDto; payables: AgingReportDto; }

export interface ProfitTrendPointDto { year: number; month: number; label: string; revenue: number; expense: number; netProfit: number; }
export interface ProfitTrendDto { points: ProfitTrendPointDto[]; }
