// ─── Purchase Order ───────────────────────────────────────────────────────

export const PO_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
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
  approvedAt: string | null;
  approvedBy: string | null;
  notes: string | null;
  totalAmount: number;
  lines: PurchaseOrderLineDto[];
}

export interface PurchaseOrderListItemDto {
  id: number;
  code: string;
  supplierId: number;
  supplierName: string;
  orderDate: string;
  expectedDeliveryDate: string | null;
  status: string;
  lineCount: number;
  totalAmount: number;
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
  lines: PurchaseOrderLineInput[];
}

export interface UpdatePurchaseOrderRequest {
  supplierId: number;
  orderDate: string;
  expectedDeliveryDate: string | null;
  deliveryWarehouseId: number | null;
  notes: string | null;
  lines: PurchaseOrderLineInput[];
}
