// ─── Phase A5b — statutory withholding (AIT / VDS / PF) ─────────────────────

export const STATUTORY_TAX_TYPES: { label: string; value: string }[] = [
  { label: 'AIT (Income Tax at Source)', value: 'Ait' },
  { label: 'VDS (VAT Deducted at Source)', value: 'Vds' },
  { label: 'Provident Fund', value: 'ProvidentFund' }
];

export interface StatutoryLiabilityDto {
  taxType: string;
  label: string;
  accountCode: string;
  outstanding: number;
}

export interface StatutoryLiabilitiesDto {
  asOfDate: string;
  items: StatutoryLiabilityDto[];
}

export interface StatutoryRemittanceDto {
  id: number;
  code: string;
  taxType: string;
  periodYear: number;
  periodMonth: number;
  amount: number;
  remittanceDate: string;
  paymentMethod: string;
  challanNo: string | null;
  notes: string | null;
}

export interface PostStatutoryRemittanceRequest {
  taxType: string;
  periodYear: number;
  periodMonth: number;
  amount: number;
  remittanceDate: string;
  paymentMethod: string;
  challanNo: string | null;
  notes: string | null;
}
