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
  aitAmount: number;                    // Phase A5b — BDT income tax withheld at source
  vdsAmount: number;                    // Phase A5b — BDT VAT deducted at source
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
  aitAmount?: number;                   // Phase A5b — BDT income tax withheld at source
  vdsAmount?: number;                   // Phase A5b — BDT VAT deducted at source
}

// Phase A5b — printable withholding (AIT/VDS) certificate for the supplier
export interface WithholdingCertificateDto {
  paymentId: number;
  paymentCode: string;
  paymentDate: string;
  supplierInvoiceCode: string;
  currencyCode: string;
  exchangeRate: number;
  grossBdt: number;
  aitAmount: number;
  vdsAmount: number;
  netPaidBdt: number;
  paymentMethod: string;
  referenceNumber: string | null;
  supplierName: string;
  supplierAddress: string | null;
  supplierBin: string | null;
  supplierTin: string | null;
  supplierPhone: string | null;
  companyName: string;
  companyAddress: string | null;
  companyBin: string | null;
  companyTin: string | null;
}
