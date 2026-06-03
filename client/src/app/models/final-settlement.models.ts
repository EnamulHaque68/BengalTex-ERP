// ─── Final Settlement (HR / Payroll v1c) ────────────────────────────────

export const FINAL_SETTLEMENT_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Paid', value: 'Paid' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export const SETTLEMENT_REASONS: { label: string; value: string }[] = [
  { label: 'Resignation', value: 'Resignation' },
  { label: 'Termination', value: 'Termination' },
  { label: 'Retirement', value: 'Retirement' },
  { label: 'Death', value: 'Death' },
  { label: 'End of Contract', value: 'EndOfContract' }
];

export const SETTLEMENT_PAYMENT_METHODS: { label: string; value: string }[] = [
  { label: 'Cash', value: 'Cash' },
  { label: 'Bank Transfer', value: 'BankTransfer' },
  { label: 'Cheque', value: 'Cheque' },
  { label: 'Mobile Banking', value: 'MobileBanking' }
];

export interface FinalSettlementDto {
  id: number;
  code: string;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  settlementDate: string;             // "YYYY-MM-DD"
  lastWorkingDate: string;
  joiningDate: string;
  yearsOfService: number;
  reason: string;
  basicSalary: number;
  proratedDays: number;
  proratedSalary: number;
  leaveEncashmentDays: number;
  leaveEncashmentAmount: number;
  gratuityAmount: number;
  otherEarnings: number;
  outstandingLoan: number;
  otherDeductions: number;
  grossPayable: number;
  totalDeductions: number;
  netPayable: number;
  status: string;
  approvedAt: string | null;
  approvedByUser: string | null;
  paidAt: string | null;
  paymentMethod: string | null;
  paymentReference: string | null;
  notes: string | null;
}

export interface FinalSettlementPreviewDto {
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  joiningDate: string;
  basicSalary: number;
  lastWorkingDate: string;
  yearsOfService: number;
  proratedDays: number;
  proratedSalary: number;
  leaveEncashmentDays: number;
  leaveEncashmentAmount: number;
  gratuityAmount: number;
  outstandingLoan: number;
  grossPayable: number;
  totalDeductions: number;
  netPayable: number;
}

export interface CreateFinalSettlementRequest {
  employeeId: number;
  lastWorkingDate: string;
  settlementDate: string;
  reason: string;
  proratedDays: number;
  proratedSalary: number;
  leaveEncashmentDays: number;
  leaveEncashmentAmount: number;
  gratuityAmount: number;
  otherEarnings: number;
  outstandingLoan: number;
  otherDeductions: number;
  notes: string | null;
}

export interface MarkFinalSettlementPaidRequest {
  paymentMethod: string;
  paymentReference: string | null;
}
