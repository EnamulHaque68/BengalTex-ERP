// ─── Purchase Requisition (PR → PO flow) ─────────────────────────────────

export const PR_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Submitted', value: 'Submitted' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Rejected', value: 'Rejected' },
  { label: 'Cancelled', value: 'Cancelled' },
  { label: 'Converted', value: 'Converted' }
];

export interface PurchaseRequisitionLineDto {
  id: number;
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  rawMaterialUnit: string | null;
  quantity: number;
  estimatedUnitPrice: number;
  lineTotal: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface PurchaseRequisitionDto {
  id: number;
  code: string;
  requisitionDate: string;
  neededByDate: string | null;
  departmentId: number | null;
  departmentName: string | null;
  departmentText: string | null;
  requestedBy: string | null;
  purpose: string | null;
  status: string;
  estimatedTotal: number;
  submittedAt: string | null;
  submittedByUser: string | null;
  decidedAt: string | null;
  decidedByUser: string | null;
  decisionNotes: string | null;
  convertedAt: string | null;
  convertedPurchaseOrderId: number | null;
  convertedPurchaseOrderCode: string | null;
  notes: string | null;
  lines: PurchaseRequisitionLineDto[];
}

export interface PurchaseRequisitionLineInput {
  rawMaterialId: number;
  quantity: number;
  estimatedUnitPrice: number;
  lineNotes: string | null;
}

export interface CreatePurchaseRequisitionRequest {
  requisitionDate: string;
  neededByDate: string | null;
  departmentId: number | null;
  departmentText: string | null;
  requestedBy: string | null;
  purpose: string | null;
  notes: string | null;
  lines: PurchaseRequisitionLineInput[];
}

export interface UpdatePurchaseRequisitionRequest extends CreatePurchaseRequisitionRequest {}

export interface ConvertPrLinePriceInput {
  purchaseRequisitionLineId: number;
  unitPrice: number;
}

export interface ConvertPrRequest {
  supplierId: number;
  orderDate: string;
  expectedDeliveryDate: string | null;
  deliveryWarehouseId: number | null;
  currencyId: number;
  exchangeRate: number;
  notes: string | null;
  linePrices: ConvertPrLinePriceInput[];
}
