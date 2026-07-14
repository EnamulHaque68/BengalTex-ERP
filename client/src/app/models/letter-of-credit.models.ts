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

// Phase A6a — LC financial events (bank-finance sub-ledger)
export const LC_EVENT_TYPES: { label: string; value: string; hint: string }[] = [
  { label: 'Margin Deposit', value: 'MarginDeposit', hint: 'Dr LC Margin / Cr Bank' },
  { label: 'Bank Charge / Commission', value: 'BankCharge', hint: 'Dr Bank Charges / Cr Bank' },
  { label: 'Document Retirement — Sight (PAD)', value: 'RetirementSight', hint: 'Dr AP / Cr Margin + Cr PAD' },
  { label: 'Document Acceptance — Usance/UPAS', value: 'AcceptanceUsance', hint: 'Dr AP / Cr Margin + Cr Acceptance' },
  { label: 'Interest', value: 'Interest', hint: 'Dr Interest Expense / Cr Bank' },
  { label: 'PAD Settlement', value: 'PadSettlement', hint: 'Dr PAD / Cr Bank' },
  { label: 'Acceptance Settlement', value: 'AcceptanceSettlement', hint: 'Dr Acceptance / Cr Bank' }
];

export interface LcFinancialEventDto {
  id: number;
  eventType: string;
  eventDate: string;
  amount: number;
  marginApplied: number;
  paymentMethod: string;
  reference: string | null;
  notes: string | null;
}

export interface LcEventsSummaryDto {
  marginBalance: number;
  padOutstanding: number;
  acceptanceOutstanding: number;
  totalCharges: number;
  totalInterest: number;
}

export interface LcEventsResultDto {
  events: LcFinancialEventDto[];
  summary: LcEventsSummaryDto;
}

export interface AddLcEventRequest {
  eventType: string;
  eventDate: string;
  amount: number;
  marginApplied: number;
  paymentMethod: string;
  reference: string | null;
  notes: string | null;
}

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
  // ── Goods-receipt utilisation summary (Area B) ──
  receivedAmount?: number;            // Σ posted-GRN received value
  remainingAmount?: number;          // amount − receivedAmount
  receivedQuantity?: number;
  orderedQuantity?: number;          // linked PO ordered qty
  utilizationPercent?: number;
  relatedGoodsReceipts?: LcGoodsReceiptRefDto[];
}

export interface LcGoodsReceiptRefDto {
  id: number;
  code: string;
  status: string;
  receiveDate: string;
  receivedQuantity: number;
  receivedAmount: number;
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
