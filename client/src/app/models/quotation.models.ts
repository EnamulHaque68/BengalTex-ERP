// ─── Quotation & Costing ──────────────────────────────────────────────────

export const QUOTATION_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Sent', value: 'Sent' },
  { label: 'Accepted', value: 'Accepted' },
  { label: 'Rejected', value: 'Rejected' },
  { label: 'Expired', value: 'Expired' },
  { label: 'Converted', value: 'Converted' }
];

export interface QuotationLineDto {
  id: number;
  productId: number;
  productCode: string;
  productName: string;
  description: string | null;
  quantity: number;
  materialCost: number;
  laborCost: number;
  machineCost: number;
  overheadCost: number;
  wastagePercent: number;
  marginPercent: number;
  unitCost: number;
  unitPrice: number;
  lineTotal: number;
  sortOrder: number;
}

export interface QuotationDto {
  id: number;
  code: string;
  customerId: number;
  customerName: string;
  quotationDate: string;
  validUntil: string | null;
  currencyId: number;
  currencyCode: string;
  exchangeRate: number;
  status: string;
  version: number;
  revisionOfId: number | null;
  totalAmount: number;
  customerReference: string | null;
  notes: string | null;
  sentAt: string | null;
  decidedAt: string | null;
  decidedBy: string | null;
  convertedSalesOrderId: number | null;
  lines: QuotationLineDto[];
}

export interface QuotationListItemDto {
  id: number;
  code: string;
  customerName: string;
  quotationDate: string;
  validUntil: string | null;
  currencyCode: string;
  totalAmount: number;
  status: string;
  version: number;
  lineCount: number;
}

export interface QuotationLineInput {
  productId: number | null;
  description: string | null;
  quantity: number;
  materialCost: number;
  laborCost: number;
  machineCost: number;
  overheadCost: number;
  wastagePercent: number;
  marginPercent: number;
}

export interface SaveQuotationRequest {
  id?: number;
  customerId: number | null;
  quotationDate: string;
  validUntil: string | null;
  currencyId: number | null;
  exchangeRate: number;
  customerReference: string | null;
  notes: string | null;
  lines: QuotationLineInput[];
}
