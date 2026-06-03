// ─── Credit / Debit Note (shared shapes) ────────────────────────────────

export const CN_DN_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Issued', value: 'Issued' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export const CN_DN_REASONS: { label: string; value: string }[] = [
  { label: 'Price Correction', value: 'PriceCorrection' },
  { label: 'Quality Allowance', value: 'QualityAllowance' },
  { label: 'Discount', value: 'Discount' },
  { label: 'Write Off', value: 'WriteOff' },
  { label: 'Other', value: 'Other' }
];

export interface CreditNoteDto {
  id: number;
  code: string;
  customerId: number;
  customerName: string;
  customerInvoiceId: number;
  customerInvoiceCode: string;
  customerInvoiceTotal: number;
  customerInvoiceAmountPaid: number;
  issueDate: string;
  reason: string;
  amount: number;
  currencyId: number;
  currencyCode: string;
  exchangeRate: number;
  status: string;
  issuedAt: string | null;
  issuedBy: string | null;
  notes: string | null;
}

export interface DebitNoteDto {
  id: number;
  code: string;
  supplierId: number;
  supplierName: string;
  supplierInvoiceId: number;
  supplierInvoiceCode: string;
  supplierInvoiceTotal: number;
  supplierInvoiceAmountPaid: number;
  issueDate: string;
  reason: string;
  amount: number;
  currencyId: number;
  currencyCode: string;
  exchangeRate: number;
  status: string;
  issuedAt: string | null;
  issuedBy: string | null;
  notes: string | null;
}

export interface CreateCreditNoteRequest {
  customerInvoiceId: number;
  issueDate: string;
  reason: string;
  amount: number;
  notes: string | null;
}

export interface UpdateCreditNoteRequest {
  issueDate: string;
  reason: string;
  amount: number;
  notes: string | null;
}

export interface CreateDebitNoteRequest {
  supplierInvoiceId: number;
  issueDate: string;
  reason: string;
  amount: number;
  notes: string | null;
}

export interface UpdateDebitNoteRequest {
  issueDate: string;
  reason: string;
  amount: number;
  notes: string | null;
}
