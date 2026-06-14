// ─── Receipt ──────────────────────────────────────────────────────────────

export const PAYMENT_METHODS: { label: string; value: string }[] = [
  { label: 'Cash', value: 'Cash' },
  { label: 'Bank Transfer', value: 'BankTransfer' },
  { label: 'Cheque', value: 'Cheque' },
  { label: 'Mobile Banking (bKash / Nagad)', value: 'MobileBanking' },
  { label: 'Other', value: 'Other' }
];

export interface ReceiptDto {
  id: number;
  code: string;
  customerInvoiceId: number;
  customerInvoiceCode: string;
  customerId: number;
  customerName: string;
  receiptDate: string;                  // DateOnly
  amount: number;
  exchangeRate: number;                 // BDT per 1 unit of invoice currency at receipt time
  paymentMethod: string;
  referenceNumber: string | null;
  notes: string | null;
}

export interface ReceiptListItemDto {
  id: number;
  code: string;
  customerInvoiceId: number;
  customerInvoiceCode: string;
  customerId: number;
  customerName: string;
  receiptDate: string;
  amount: number;
  paymentMethod: string;
  referenceNumber: string | null;
}

export interface CreateReceiptRequest {
  customerInvoiceId: number;
  receiptDate: string;
  amount: number;
  paymentMethod: string;
  referenceNumber: string | null;
  notes: string | null;
  exchangeRate?: number | null;         // null → backend uses the invoice's locked rate (no FX)
}
