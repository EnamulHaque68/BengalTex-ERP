// ─── Phase A6c — bank treasury facilities (loan / OD / FDR) ─────────────────

export const FACILITY_TYPES: { label: string; value: string }[] = [
  { label: 'Term Loan', value: 'TermLoan' },
  { label: 'Overdraft / Cash Credit', value: 'OverdraftCC' },
  { label: 'Fixed Deposit (FDR)', value: 'Fdr' }
];

export const FACILITY_STATUSES: { label: string; value: string }[] = [
  { label: 'Active', value: 'Active' },
  { label: 'Closed', value: 'Closed' }
];

export interface FacilityEventType { label: string; value: string; family: 'loan' | 'fdr'; hint: string; }

export const FACILITY_EVENT_TYPES: FacilityEventType[] = [
  { label: 'Drawdown', value: 'Drawdown', family: 'loan', hint: 'Dr Bank / Cr Bank Loan' },
  { label: 'Interest Charge', value: 'InterestCharge', family: 'loan', hint: 'Dr Interest Exp / Cr Bank' },
  { label: 'Principal Repayment', value: 'PrincipalRepayment', family: 'loan', hint: 'Dr Bank Loan / Cr Bank' },
  { label: 'FDR Placement', value: 'FdrPlacement', family: 'fdr', hint: 'Dr FDR / Cr Bank' },
  { label: 'FDR Interest Income', value: 'FdrInterestIncome', family: 'fdr', hint: 'Dr Bank / Cr Other Income' },
  { label: 'FDR Encashment', value: 'FdrEncashment', family: 'fdr', hint: 'Dr Bank / Cr FDR' }
];

export interface BankFacilityDto {
  id: number;
  code: string;
  facilityType: string;
  bankName: string;
  accountReference: string | null;
  amount: number;
  interestRate: number;
  startDate: string;
  maturityDate: string | null;
  status: string;
  notes: string | null;
}

export interface BankFacilityEventDto {
  id: number;
  eventType: string;
  eventDate: string;
  amount: number;
  paymentMethod: string;
  reference: string | null;
  notes: string | null;
}

export interface BankFacilitySummaryDto {
  loanOutstanding: number;
  fdrBalance: number;
  totalInterestPaid: number;
  totalInterestIncome: number;
}

export interface BankFacilityDetailDto {
  facility: BankFacilityDto;
  events: BankFacilityEventDto[];
  summary: BankFacilitySummaryDto;
}

export interface CreateBankFacilityRequest {
  facilityType: string;
  bankName: string;
  accountReference: string | null;
  amount: number;
  interestRate: number;
  startDate: string;
  maturityDate: string | null;
  notes: string | null;
}

export interface AddBankFacilityEventRequest {
  eventType: string;
  eventDate: string;
  amount: number;
  paymentMethod: string;
  reference: string | null;
  notes: string | null;
}
