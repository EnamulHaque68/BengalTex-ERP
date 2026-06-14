// ─── Payment (AP) ─────────────────────────────────────────────────────────

export const PAYMENT_METHODS: { label: string; value: string }[] = [
  { label: 'Cash', value: 'Cash' },
  { label: 'Bank Transfer', value: 'BankTransfer' },
  { label: 'Cheque', value: 'Cheque' },
  { label: 'Mobile Banking (bKash / Nagad)', value: 'MobileBanking' },
  { label: 'Other', value: 'Other' }
];

export interface PaymentDto {
  id: number;
  code: string;
  supplierInvoiceId: number;
  supplierInvoiceCode: string;
  supplierId: number;
  supplierName: string;
  paymentDate: string;                  // DateOnly
  amount: number;
  exchangeRate: number;                 // BDT per 1 unit of invoice currency at payment time
  paymentMethod: string;
  referenceNumber: string | null;
  notes: string | null;
}

export interface PaymentListItemDto {
  id: number;
  code: string;
  supplierInvoiceId: number;
  supplierInvoiceCode: string;
  supplierId: number;
  supplierName: string;
  paymentDate: string;
  amount: number;
  paymentMethod: string;
  referenceNumber: string | null;
}

export interface CreatePaymentRequest {
  supplierInvoiceId: number;
  paymentDate: string;
  amount: number;
  paymentMethod: string;
  referenceNumber: string | null;
  notes: string | null;
  exchangeRate?: number | null;         // null → backend uses the invoice's locked rate (no FX)
}
