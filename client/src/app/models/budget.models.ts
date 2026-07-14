// ─── Phase A7a — budgeting ─────────────────────────────────────────────────

export const BUDGET_MONTHS = ['M1','M2','M3','M4','M5','M6','M7','M8','M9','M10','M11','M12'] as const;

export interface BudgetDto {
  id: number;
  code: string;
  financialYearId: number;
  financialYearCode: string;
  name: string;
  status: string;
  lineCount: number;
  annualTotal: number;
}

export interface BudgetLineDto {
  id: number;
  accountId: number;
  accountCode: string;
  accountName: string;
  costCenterId: number | null;
  costCenterName: string | null;
  m1: number; m2: number; m3: number; m4: number; m5: number; m6: number;
  m7: number; m8: number; m9: number; m10: number; m11: number; m12: number;
  total: number;
}

export interface BudgetDetailDto {
  id: number;
  code: string;
  financialYearId: number;
  financialYearCode: string;
  name: string;
  status: string;
  notes: string | null;
  lines: BudgetLineDto[];
}

export interface BudgetLineInput {
  accountId: number;
  costCenterId: number | null;
  m1: number; m2: number; m3: number; m4: number; m5: number; m6: number;
  m7: number; m8: number; m9: number; m10: number; m11: number; m12: number;
}

export interface BudgetVarianceRowDto {
  accountId: number;
  accountCode: string;
  accountName: string;
  accountType: string;
  budget: number;
  actual: number;
  variance: number;
  variancePct: number;
}

export interface BudgetVarianceReportDto {
  budgetId: number;
  budgetCode: string;
  fromMonth: number;
  toMonth: number;
  rows: BudgetVarianceRowDto[];
  totalBudget: number;
  totalActual: number;
  totalVariance: number;
}
