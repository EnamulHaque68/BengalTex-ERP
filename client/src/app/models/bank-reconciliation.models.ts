// ─── Bank Reconciliation ─────────────────────────────────────────────────

export type BankStatementLineStatus = 'Unmatched' | 'Matched' | 'Excluded';

export interface BankStatementListItemDto {
  id: number;
  code: string;
  bankAccountId: number;
  bankAccountName: string;
  statementDate: string;
  periodFromDate: string;
  periodToDate: string;
  openingBalance: number;
  closingBalance: number;
  isReconciled: boolean;
  reconciledAt: string | null;
  lineCount: number;
  matchedCount: number;
  unmatchedCount: number;
}

export interface BankStatementLineDto {
  id: number;
  bankStatementId: number;
  transactionDate: string;
  description: string;
  referenceNumber: string | null;
  amount: number;
  status: BankStatementLineStatus;
  matchedJournalLineId: number | null;
  matchedJournalEntryCode: string | null;
  matchedJournalNarration: string | null;
  matchedAt: string | null;
  matchedBy: string | null;
  notes: string | null;
}

export interface BankStatementDto {
  id: number;
  code: string;
  bankAccountId: number;
  bankAccountName: string;
  ledgerAccountId: number | null;
  ledgerAccountCode: string | null;
  ledgerAccountName: string | null;
  statementDate: string;
  periodFromDate: string;
  periodToDate: string;
  openingBalance: number;
  closingBalance: number;
  matchedAmount: number;
  computedClosing: number;
  balancesMatch: boolean;
  isReconciled: boolean;
  reconciledAt: string | null;
  reconciledBy: string | null;
  notes: string | null;
  lines: BankStatementLineDto[];
}

export interface UnmatchedJournalLineDto {
  id: number;
  journalEntryId: number;
  journalEntryCode: string;
  entryDate: string;
  narration: string;
  sourceType: string | null;
  sourceCode: string | null;
  amount: number;
  debit: number;
  credit: number;
}

export interface CreateBankStatementRequest {
  bankAccountId: number;
  statementDate: string;
  periodFromDate: string;
  periodToDate: string;
  openingBalance: number;
  closingBalance: number;
  notes: string | null;
}

export interface UpdateBankStatementRequest {
  statementDate: string;
  periodFromDate: string;
  periodToDate: string;
  openingBalance: number;
  closingBalance: number;
  notes: string | null;
}

export interface SaveStatementLineRequest {
  transactionDate: string;
  description: string;
  referenceNumber: string | null;
  amount: number;
  notes: string | null;
}

export interface ImportBankStatementLineInput {
  transactionDate: string;
  description: string;
  referenceNumber: string | null;
  amount: number;          // signed: + inflow, − outflow
}

export interface ImportBankStatementRequest {
  bankAccountId: number;
  statementDate: string;
  periodFromDate: string;
  periodToDate: string;
  openingBalance: number;  // 0 = auto-derive from prior statement
  closingBalance: number;  // 0 = auto-compute = opening + Σ amounts
  notes: string | null;
  lines: ImportBankStatementLineInput[];
}
