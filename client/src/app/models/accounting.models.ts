// ─── Accounting (Chart of Accounts + Journals + Reports) ──────────────────────

export const ACCOUNT_TYPES: { label: string; value: string }[] = [
  { label: 'Asset', value: 'Asset' },
  { label: 'Liability', value: 'Liability' },
  { label: 'Equity', value: 'Equity' },
  { label: 'Income', value: 'Income' },
  { label: 'Expense', value: 'Expense' }
];

export const JOURNAL_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Posted', value: 'Posted' }
];

export interface AccountDto {
  id: number;
  code: string;
  name: string;
  accountType: string;
  normalBalance: string;        // Debit | Credit
  isGroup: boolean;
  parentAccountId: number | null;
  parentAccountName: string | null;
  isSystem: boolean;
  isActive: boolean;
  description: string | null;
}

export interface CreateAccountRequest {
  code: string;
  name: string;
  accountType: string;
  isGroup: boolean;
  parentAccountId: number | null;
  description: string | null;
}

export interface UpdateAccountRequest {
  id: number;
  code: string;
  name: string;
  accountType: string;
  isGroup: boolean;
  parentAccountId: number | null;
  isActive: boolean;
  description: string | null;
}

export interface JournalEntryLineDto {
  id: number;
  accountId: number;
  accountCode: string;
  accountName: string;
  debit: number;
  credit: number;
  lineNarration: string | null;
  sortOrder: number;
}

export interface JournalEntryDto {
  id: number;
  code: string;
  entryDate: string;
  reference: string | null;
  narration: string | null;
  status: string;
  sourceType: string | null;
  sourceId: number | null;
  sourceCode: string | null;
  postedAt: string | null;
  postedBy: string | null;
  totalDebit: number;
  totalCredit: number;
  lines: JournalEntryLineDto[];
}

export interface JournalEntryListItemDto {
  id: number;
  code: string;
  entryDate: string;
  reference: string | null;
  narration: string | null;
  status: string;
  amount: number;
  lineCount: number;
  sourceType: string | null;
  sourceCode: string | null;
}

export interface JournalEntryLineInput {
  accountId: number | null;
  debit: number;
  credit: number;
  lineNarration: string | null;
}

export interface SaveJournalEntryRequest {
  id?: number;
  entryDate: string;
  reference: string | null;
  narration: string | null;
  lines: JournalEntryLineInput[];
}

// ── Reports ──
export interface TrialBalanceRowDto {
  accountId: number;
  accountCode: string;
  accountName: string;
  accountType: string;
  debitBalance: number;
  creditBalance: number;
}

export interface TrialBalanceDto {
  asOfDate: string;
  rows: TrialBalanceRowDto[];
  totalDebit: number;
  totalCredit: number;
  isBalanced: boolean;
}

export interface GeneralLedgerLineDto {
  entryDate: string;
  journalCode: string;
  narration: string | null;
  debit: number;
  credit: number;
  runningBalance: number;
}

export interface GeneralLedgerDto {
  accountId: number;
  accountCode: string;
  accountName: string;
  normalBalance: string;
  fromDate: string;
  toDate: string;
  openingBalance: number;
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
  lines: GeneralLedgerLineDto[];
}
