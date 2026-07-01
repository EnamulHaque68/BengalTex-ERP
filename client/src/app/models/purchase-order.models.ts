// ─── Purchase Order ───────────────────────────────────────────────────────

export const PO_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Pending Approval', value: 'PendingApproval' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Sent', value: 'Sent' },
  { label: 'Partially Received', value: 'PartiallyReceived' },
  { label: 'Received', value: 'Received' },
  { label: 'Closed', value: 'Closed' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface PurchaseOrderLineDto {
  id: number;
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  unitOfMeasureCode: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  receivedQuantity: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface PurchaseOrderDto {
  id: number;
  code: string;
  supplierId: number;
  supplierCode: string;
  supplierName: string;
  orderDate: string;                  // DateOnly → "YYYY-MM-DD"
  expectedDeliveryDate: string | null;
  deliveryWarehouseId: number | null;
  deliveryWarehouseName: string | null;
  status: string;
  currencyId: number;
  currencyCode: string;
  currencySymbol: string;
  exchangeRate: number;
  approvedAt: string | null;
  approvedBy: string | null;
  notes: string | null;
  totalAmount: number;                // document currency
  baseTotalAmount: number;            // BDT
  lines: PurchaseOrderLineDto[];
  // Source traceability (Area C)
  purchaseRequisitionId: number | null;
  purchaseRequisitionCode: string | null;
  supplierQuotationId: number | null;
  supplierQuotationCode: string | null;
}

export interface PurchaseOrderListItemDto {
  id: number;
  code: string;
  supplierId: number;
  supplierName: string;
  orderDate: string;
  expectedDeliveryDate: string | null;
  status: string;
  currencyCode: string;
  exchangeRate: number;
  lineCount: number;
  totalAmount: number;                // document currency
  baseTotalAmount: number;            // BDT
}

export interface PurchaseOrderLineInput {
  rawMaterialId: number;
  quantity: number;
  unitPrice: number;
  lineNotes: string | null;
}

export interface CreatePurchaseOrderRequest {
  supplierId: number;
  orderDate: string;
  expectedDeliveryDate: string | null;
  deliveryWarehouseId: number | null;
  notes: string | null;
  currencyId: number;
  exchangeRate: number;
  lines: PurchaseOrderLineInput[];
}

export interface UpdatePurchaseOrderRequest {
  supplierId: number;
  orderDate: string;
  expectedDeliveryDate: string | null;
  deliveryWarehouseId: number | null;
  notes: string | null;
  currencyId: number;
  exchangeRate: number;
  lines: PurchaseOrderLineInput[];
}
