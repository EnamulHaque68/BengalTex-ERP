// ─── Delivery Note ────────────────────────────────────────────────────────

export const DN_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Posted', value: 'Posted' }
];

export interface DeliveryNoteLineDto {
  id: number;
  salesOrderLineId: number;
  productId: number;
  productCode: string;
  productName: string;
  unitOfMeasureCode: string;
  orderedQuantity: number;
  dispatchedQuantity: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface DeliveryNoteDto {
  id: number;
  code: string;
  salesOrderId: number;
  salesOrderCode: string;
  customerId: number;
  customerName: string;
  dispatchDate: string;                  // DateOnly → "YYYY-MM-DD"
  dispatchWarehouseId: number;
  dispatchWarehouseName: string;
  status: string;
  vehicleNumber: string | null;
  driverContact: string | null;
  deliveryAddress: string | null;
  postedAt: string | null;
  postedBy: string | null;
  notes: string | null;
  lines: DeliveryNoteLineDto[];
}

export interface DeliveryNoteListItemDto {
  id: number;
  code: string;
  salesOrderId: number;
  salesOrderCode: string;
  customerName: string;
  dispatchDate: string;
  dispatchWarehouseCode: string;
  status: string;
  lineCount: number;
}

export interface DeliveryNoteLineInput {
  salesOrderLineId: number;
  dispatchedQuantity: number;
  lineNotes: string | null;
}

export interface CreateDeliveryNoteRequest {
  salesOrderId: number;
  dispatchDate: string;
  dispatchWarehouseId: number;
  vehicleNumber: string | null;
  driverContact: string | null;
  deliveryAddress: string | null;
  notes: string | null;
  lines: DeliveryNoteLineInput[];
}

export interface UpdateDeliveryNoteRequest {
  dispatchDate: string;
  dispatchWarehouseId: number;
  vehicleNumber: string | null;
  driverContact: string | null;
  deliveryAddress: string | null;
  notes: string | null;
  lines: DeliveryNoteLineInput[];
}
