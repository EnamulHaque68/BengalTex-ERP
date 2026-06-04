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
  // BD payroll breakdown — earnings
  houseRent: number;
  medical: number;
  transport: number;
  foodAllowance: number;
  festivalBonus: number;
  // BD payroll breakdown — deductions
  pfEmployee: number;
  pfEmployer: number;
  incomeTax: number;
  loanDeduction: number;
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
  houseRent: number;
  medical: number;
  transport: number;
  foodAllowance: number;
  festivalBonus: number;
  pfEmployee: number;
  pfEmployer: number;
  incomeTax: number;
  loanDeduction: number;
  notes: string | null;
}

export interface PayslipPrintDto {
  id: number;
  payslipCode: string;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  designation: string | null;
  department: string | null;
  employeePhone: string | null;
  employeeNationalId: string | null;
  joiningDate: string | null;
  employmentType: string | null;
  year: number;
  month: number;
  monthName: string;
  presentDays: number;
  absentDays: number;
  leaveDays: number;
  overtimeHours: number;
  basicSalary: number;
  houseRent: number;
  medical: number;
  transport: number;
  foodAllowance: number;
  festivalBonus: number;
  allowances: number;
  overtimeAmount: number;
  grossPay: number;
  pfEmployee: number;
  pfEmployer: number;
  incomeTax: number;
  loanDeduction: number;
  otherDeductions: number;
  totalDeductions: number;
  netPay: number;
  status: string;
  paidAt: string | null;
  notes: string | null;
  bankName: string | null;
  bankBranch: string | null;
  bankAccountNumber: string | null;
  companyName: string;
  companyShortName: string | null;
  companyAddressLine1: string | null;
  companyAddressLine2: string | null;
  companyCity: string | null;
  companyDistrict: string | null;
  companyPostalCode: string | null;
  companyPhone: string | null;
  companyEmail: string | null;
  companyTaxNumber: string | null;
  companyLogoUrl: string | null;
}
