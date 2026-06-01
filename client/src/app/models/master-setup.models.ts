// ─── Master Setup ─────────────────────────────────────────────────────────

export interface DepartmentDto {
  id: number;
  code: string | null;
  name: string;
  parentDepartmentId: number | null;
  parentDepartmentName: string | null;
  headEmployeeId: number | null;
  headEmployeeName: string | null;
  description: string | null;
  isActive: boolean;
}

export interface SaveDepartmentRequest {
  code: string | null;
  name: string;
  parentDepartmentId: number | null;
  headEmployeeId: number | null;
  description: string | null;
  isActive?: boolean;
}

export interface DesignationDto {
  id: number;
  code: string | null;
  name: string;
  gradeLevel: number | null;
  description: string | null;
  isActive: boolean;
}

export interface SaveDesignationRequest {
  code: string | null;
  name: string;
  gradeLevel: number | null;
  description: string | null;
  isActive?: boolean;
}

export type DayOfWeek = 'Sunday' | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday';

export const DAYS_OF_WEEK: { label: string; value: DayOfWeek }[] = [
  { label: 'Sunday', value: 'Sunday' },
  { label: 'Monday', value: 'Monday' },
  { label: 'Tuesday', value: 'Tuesday' },
  { label: 'Wednesday', value: 'Wednesday' },
  { label: 'Thursday', value: 'Thursday' },
  { label: 'Friday', value: 'Friday' },
  { label: 'Saturday', value: 'Saturday' }
];

export interface ShiftDto {
  id: number;
  code: string;
  name: string;
  startTime: string;          // "HH:mm"
  endTime: string;
  weekendDayOfWeek: DayOfWeek;
  secondWeekendDayOfWeek: DayOfWeek | null;
  description: string | null;
  isActive: boolean;
}

export interface CreateShiftRequest {
  code: string;
  name: string;
  startTime: string;
  endTime: string;
  weekendDayOfWeek: DayOfWeek;
  secondWeekendDayOfWeek: DayOfWeek | null;
  description: string | null;
}

export interface UpdateShiftRequest {
  name: string;
  startTime: string;
  endTime: string;
  weekendDayOfWeek: DayOfWeek;
  secondWeekendDayOfWeek: DayOfWeek | null;
  description: string | null;
  isActive: boolean;
}

export type BankAccountType = 'Current' | 'Savings' | 'STD' | 'FixedDeposit' | 'Other';

export const BANK_ACCOUNT_TYPES: { label: string; value: BankAccountType }[] = [
  { label: 'Current', value: 'Current' },
  { label: 'Savings', value: 'Savings' },
  { label: 'STD (Short Term Deposit)', value: 'STD' },
  { label: 'Fixed Deposit', value: 'FixedDeposit' },
  { label: 'Other', value: 'Other' }
];

export interface BankAccountDto {
  id: number;
  accountName: string;
  bankName: string;
  branchName: string | null;
  accountNumber: string;
  accountType: BankAccountType;
  routingNumber: string | null;
  swiftCode: string | null;
  currency: string;
  ledgerAccountId: number | null;
  ledgerAccountCode: string | null;
  ledgerAccountName: string | null;
  notes: string | null;
  isActive: boolean;
}

export interface SaveBankAccountRequest {
  accountName: string;
  bankName: string;
  branchName: string | null;
  accountNumber: string;
  accountType: BankAccountType;
  routingNumber: string | null;
  swiftCode: string | null;
  currency: string;
  ledgerAccountId: number | null;
  notes: string | null;
  isActive?: boolean;
}
