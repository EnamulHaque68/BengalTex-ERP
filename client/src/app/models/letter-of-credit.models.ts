// ─── Letter of Credit (Banking) ───────────────────────────────────────────

export const LC_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Open', value: 'Open' },
  { label: 'Shipped', value: 'Shipped' },
  { label: 'Settled', value: 'Settled' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export const LC_TYPES: { label: string; value: string }[] = [
  { label: 'Import LC', value: 'Import' },
  { label: 'Back-to-Back LC', value: 'BackToBack' }
];

export interface LetterOfCreditDto {
  id: number;
  code: string;
  lcNumber: string;
  issuingBank: string;
  supplierId: number;
  supplierName: string;
  purchaseOrderId: number | null;
  purchaseOrderCode: string | null;
  currencyId: number;
  currencyCode: string;
  currencySymbol: string;
  exchangeRate: number;
  amount: number;
  baseAmount: number;
  issueDate: string;
  expiryDate: string;
  tenorDays: number;
  status: string;
  type: string;                    // "Import" | "BackToBack"
  masterLcReference: string | null;
  masterLcBuyer: string | null;
  shipmentDate: string | null;
  settlementDate: string | null;
  notes: string | null;
}

export interface LetterOfCreditListItemDto {
  id: number;
  code: string;
  lcNumber: string;
  issuingBank: string;
  supplierName: string;
  currencyCode: string;
  amount: number;
  baseAmount: number;
  issueDate: string;
  expiryDate: string;
  status: string;
  type: string;
}

export interface CreateLetterOfCreditRequest {
  code: string | null;
  lcNumber: string;
  issuingBank: string;
  supplierId: number;
  purchaseOrderId: number | null;
  currencyId: number;
  exchangeRate: number;
  amount: number;
  issueDate: string;
  expiryDate: string;
  tenorDays: number;
  notes: string | null;
  type: string;
  masterLcReference: string | null;
  masterLcBuyer: string | null;
}

export interface UpdateLetterOfCreditRequest {
  lcNumber: string;
  issuingBank: string;
  supplierId: number;
  purchaseOrderId: number | null;
  currencyId: number;
  exchangeRate: number;
  amount: number;
  issueDate: string;
  expiryDate: string;
  tenorDays: number;
  notes: string | null;
  type: string;
  masterLcReference: string | null;
  masterLcBuyer: string | null;
}
