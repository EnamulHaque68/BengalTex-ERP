// ─── Payroll ──────────────────────────────────────────────────────────────

export const PAYSLIP_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Paid', value: 'Paid' }
];

export const MONTHS: { label: string; value: number }[] = [
  { label: 'January', value: 1 }, { label: 'February', value: 2 }, { label: 'March', value: 3 },
  { label: 'April', value: 4 }, { label: 'May', value: 5 }, { label: 'June', value: 6 },
  { label: 'July', value: 7 }, { label: 'August', value: 8 }, { label: 'September', value: 9 },
  { label: 'October', value: 10 }, { label: 'November', value: 11 }, { label: 'December', value: 12 }
];

export interface PayslipDto {
  id: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  year: number;
  month: number;
  basicSalary: number;
  presentDays: number;
  absentDays: number;
  leaveDays: number;
  overtimeHours: number;
  overtimeAmount: number;
  allowances: number;
  deductions: number;
  grossPay: number;
  netPay: number;
  status: string;
  paidAt: string | null;
  notes: string | null;
}

export interface GeneratePayrollRequest {
  year: number;
  month: number;
}

export interface UpdatePayslipRequest {
  overtimeAmount: number;
  allowances: number;
  deductions: number;
  notes: string | null;
}
