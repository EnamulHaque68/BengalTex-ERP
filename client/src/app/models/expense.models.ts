// ─── Expense Management ───────────────────────────────────────────────────

export const EXPENSE_PAYMENT_METHODS: { label: string; value: string }[] = [
  { label: 'Cash', value: 'Cash' },
  { label: 'Bank Transfer', value: 'BankTransfer' },
  { label: 'Cheque', value: 'Cheque' },
  { label: 'Mobile Banking', value: 'MobileBanking' },
  { label: 'Other', value: 'Other' }
];

export const EXPENSE_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Pending Approval', value: 'PendingApproval' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface ExpenseCategoryDto {
  id: number;
  name: string;
  ledgerAccountId: number | null;
  ledgerAccountCode: string | null;
  ledgerAccountName: string | null;
  isActive: boolean;
  description: string | null;
}

export interface SaveExpenseCategoryRequest {
  id?: number;
  name: string;
  ledgerAccountId: number | null;
  isActive?: boolean;
  description: string | null;
}

export interface ExpenseDto {
  id: number;
  code: string;
  expenseDate: string;
  expenseCategoryId: number;
  expenseCategoryName: string;
  amount: number;
  paymentMethod: string;
  payee: string | null;
  referenceNumber: string | null;
  description: string | null;
  status: string;
  approvedAt: string | null;
  approvedBy: string | null;
}

export interface ExpenseListItemDto {
  id: number;
  code: string;
  expenseDate: string;
  expenseCategoryName: string;
  amount: number;
  paymentMethod: string;
  payee: string | null;
  status: string;
}

export interface SaveExpenseRequest {
  id?: number;
  expenseDate: string;
  expenseCategoryId: number | null;
  amount: number;
  paymentMethod: string;
  payee: string | null;
  referenceNumber: string | null;
  description: string | null;
}

export interface ExpenseSummaryRowDto {
  expenseCategoryId: number;
  expenseCategoryName: string;
  amount: number;
  count: number;
}

export interface ExpenseSummaryDto {
  fromDate: string;
  toDate: string;
  rows: ExpenseSummaryRowDto[];
  totalAmount: number;
}
