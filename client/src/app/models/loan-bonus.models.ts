// ─── Employee Loan + Festival Bonus ───────────────────────────────────────

export type EmployeeLoanStatus = 'Active' | 'Closed' | 'Cancelled';

export const LOAN_STATUSES: { label: string; value: EmployeeLoanStatus }[] = [
  { label: 'Active', value: 'Active' },
  { label: 'Closed', value: 'Closed' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface EmployeeLoanDto {
  id: number;
  code: string;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  issuedDate: string;
  principal: number;
  emiAmount: number;
  tenureMonths: number;
  startYearMonth: number;     // YYYYMM
  outstandingPrincipal: number;
  totalRepaid: number;
  status: EmployeeLoanStatus;
  notes: string | null;
}

export interface CreateEmployeeLoanRequest {
  employeeId: number;
  issuedDate: string;
  principal: number;
  emiAmount: number;
  tenureMonths: number;
  startYearMonth: number;
  notes: string | null;
}

export interface UpdateEmployeeLoanRequest {
  emiAmount: number;
  tenureMonths: number;
  startYearMonth: number;
  notes: string | null;
}

export type FestivalBonusType = 'EidUlFitr' | 'EidUlAzha' | 'PohelaBoishakh' | 'Other';
export type FestivalBonusStatus = 'Draft' | 'Paid' | 'Cancelled';

export const FESTIVAL_BONUS_TYPES: { label: string; value: FestivalBonusType }[] = [
  { label: 'Eid-ul-Fitr', value: 'EidUlFitr' },
  { label: 'Eid-ul-Azha', value: 'EidUlAzha' },
  { label: 'Pohela Boishakh', value: 'PohelaBoishakh' },
  { label: 'Other', value: 'Other' }
];

export const FESTIVAL_BONUS_STATUSES: { label: string; value: FestivalBonusStatus }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Paid', value: 'Paid' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface FestivalBonusDto {
  id: number;
  code: string;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  bonusYear: number;
  bonusType: FestivalBonusType;
  amount: number;
  status: FestivalBonusStatus;
  paymentMethod: string;
  paidAt: string | null;
  paidBy: string | null;
  notes: string | null;
}

export interface BulkCreateFestivalBonusRequest {
  bonusYear: number;
  bonusType: FestivalBonusType;
  amount: number;
  notes: string | null;
}

export interface UpdateFestivalBonusRequest {
  amount: number;
  paymentMethod: string;
  notes: string | null;
}
