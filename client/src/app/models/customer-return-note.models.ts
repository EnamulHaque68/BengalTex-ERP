// ─── Customer Return Note (CRN) ───────────────────────────────────────────

export const CRN_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft',  value: 'Draft' },
  { label: 'Posted', value: 'Posted' }
];

export interface CustomerReturnNoteLineDto {
  id: number;
  deliveryNoteLineId: number;
  productId: number;
  productCode: string;
  productName: string;
  unitOfMeasureCode: string;
  dispatchedQuantity: number;
  previouslyReturnedQuantity: number;
  returnedQuantity: number;
  availableForReturn: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface CustomerReturnNoteDto {
  id: number;
  code: string;
  deliveryNoteId: number;
  deliveryNoteCode: string;
  salesOrderId: number;
  salesOrderCode: string;
  customerId: number;
  customerName: string;
  returnDate: string;                   // DateOnly
  returnWarehouseId: number;
  returnWarehouseCode: string;
  returnWarehouseName: string;
  status: string;
  vehicleNumber: string | null;
  reason: string | null;
  postedAt: string | null;
  postedBy: string | null;
  notes: string | null;
  lines: CustomerReturnNoteLineDto[];
}

export interface CustomerReturnNoteListItemDto {
  id: number;
  code: string;
  deliveryNoteId: number;
  deliveryNoteCode: string;
  customerId: number;
  customerName: string;
  returnDate: string;
  returnWarehouseId: number;
  returnWarehouseName: string;
  status: string;
  lineCount: number;
  totalReturnedQuantity: number;
}

export interface CustomerReturnNoteLineInput {
  deliveryNoteLineId: number;
  returnedQuantity: number;
  lineNotes: string | null;
}

export interface CreateCustomerReturnNoteRequest {
  deliveryNoteId: number;
  returnWarehouseId: number;
  returnDate: string;
  vehicleNumber: string | null;
  reason: string | null;
  notes: string | null;
  lines: CustomerReturnNoteLineInput[];
}

export interface UpdateCustomerReturnNoteRequest {
  returnWarehouseId: number;
  returnDate: string;
  vehicleNumber: string | null;
  reason: string | null;
  notes: string | null;
  lines: CustomerReturnNoteLineInput[];
}
