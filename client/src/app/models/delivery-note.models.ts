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
  returnedQuantity: number;            // already-returned via posted CRNs
  invoicedQuantity: number;            // already billed onto customer invoices
  remainingToInvoice: number;          // dispatchedQuantity − invoicedQuantity
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
  plannedDeliveryDate: string | null;
  vehicleNumber: string | null;
  transportCompany: string | null;
  driverName: string | null;
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
  deliveredQuantity: number;           // Σ dispatched across lines
  invoicedQuantity: number;            // Σ already invoiced across lines
  remainingToInvoice: number;          // delivered − invoiced
  invoiceState: string;                // NotInvoiced | PartiallyInvoiced | FullyInvoiced
}

export interface DeliveryNoteLineInput {
  salesOrderLineId: number;
  dispatchedQuantity: number;
  lineNotes: string | null;
}

/** One DN line + how much of its remaining qty to invoice this time. */
export interface DeliveryInvoiceLineInput {
  deliveryNoteLineId: number;
  quantity: number;
}

/** Partial delivery → invoice request body. */
export interface CreateInvoiceFromDeliveryNoteRequest {
  vatRate: number;
  lines: DeliveryInvoiceLineInput[];
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
  plannedDeliveryDate: string | null;
  transportCompany: string | null;
  driverName: string | null;
}

export interface UpdateDeliveryNoteRequest {
  dispatchDate: string;
  dispatchWarehouseId: number;
  vehicleNumber: string | null;
  driverContact: string | null;
  deliveryAddress: string | null;
  notes: string | null;
  lines: DeliveryNoteLineInput[];
  plannedDeliveryDate: string | null;
  transportCompany: string | null;
  driverName: string | null;
}
