// ─── Supplier Quotation (RFQ) ───────────────────────────────────────────────

export const SUPPLIER_QUOTATION_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Submitted', value: 'Submitted' },
  { label: 'Selected', value: 'Selected' },
  { label: 'Rejected', value: 'Rejected' }
];

export interface SupplierQuotationLineInput {
  rawMaterialId: number;
  quantity: number;
  unitPrice: number;
  leadTimeDays: number | null;
  lineNotes: string | null;
}

export interface SupplierQuotationLineDto {
  id: number;
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  rawMaterialUnit: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  leadTimeDays: number | null;
  sortOrder: number;
  lineNotes: string | null;
}

export interface SupplierQuotationDto {
  id: number;
  code: string;
  quotationDate: string;
  supplierId: number;
  supplierName: string;
  purchaseRequisitionId: number | null;
  purchaseRequisitionCode: string | null;
  currencyId: number;
  currencyCode: string;
  exchangeRate: number;
  validUntil: string | null;
  status: string;
  decidedAt: string | null;
  decidedBy: string | null;
  convertedPurchaseOrderId: number | null;
  convertedAt: string | null;
  notes: string | null;
  totalAmount: number;
  totalAmountBase: number;
  lines: SupplierQuotationLineDto[];
}

export interface SupplierQuotationListItemDto {
  id: number;
  code: string;
  quotationDate: string;
  supplierName: string;
  purchaseRequisitionCode: string | null;
  currencyCode: string;
  status: string;
  lineCount: number;
  totalAmount: number;
  totalAmountBase: number;
}

export interface SaveSupplierQuotationRequest {
  quotationDate: string;
  supplierId: number;
  purchaseRequisitionId: number | null;
  currencyId: number;
  exchangeRate: number;
  validUntil: string | null;
  notes: string | null;
  lines: SupplierQuotationLineInput[];
}

// ── Comparison matrix ──
export interface QuotationComparisonSupplierDto {
  supplierQuotationId: number;
  code: string;
  supplierName: string;
  currencyCode: string;
  exchangeRate: number;
  status: string;
  validUntil: string | null;
  totalBase: number;
  isLowestTotal: boolean;
}

export interface QuotationComparisonCellDto {
  supplierQuotationId: number;
  hasQuote: boolean;
  unitPrice: number;
  unitPriceBase: number;
  leadTimeDays: number | null;
  lineTotalBase: number;
  isLowest: boolean;
}

export interface QuotationComparisonRowDto {
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  quantity: number;
  cells: QuotationComparisonCellDto[];
}

export interface QuotationComparisonDto {
  purchaseRequisitionId: number;
  purchaseRequisitionCode: string;
  suppliers: QuotationComparisonSupplierDto[];
  rows: QuotationComparisonRowDto[];
}
