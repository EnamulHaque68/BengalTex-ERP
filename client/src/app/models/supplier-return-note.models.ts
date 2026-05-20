// ─── Supplier Return Note (SRN) ───────────────────────────────────────────

export const SRN_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft',  value: 'Draft' },
  { label: 'Posted', value: 'Posted' }
];

export interface SupplierReturnNoteLineDto {
  id: number;
  goodsReceiptLineId: number;
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  unitOfMeasureCode: string;
  receivedQuantity: number;
  previouslyReturnedQuantity: number;
  returnedQuantity: number;
  availableForReturn: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface SupplierReturnNoteDto {
  id: number;
  code: string;
  goodsReceiptNoteId: number;
  goodsReceiptNoteCode: string;
  purchaseOrderId: number;
  purchaseOrderCode: string;
  supplierId: number;
  supplierName: string;
  returnDate: string;
  returnFromWarehouseId: number;
  returnFromWarehouseCode: string;
  returnFromWarehouseName: string;
  status: string;
  vehicleNumber: string | null;
  reason: string | null;
  postedAt: string | null;
  postedBy: string | null;
  notes: string | null;
  lines: SupplierReturnNoteLineDto[];
}

export interface SupplierReturnNoteListItemDto {
  id: number;
  code: string;
  goodsReceiptNoteId: number;
  goodsReceiptNoteCode: string;
  supplierId: number;
  supplierName: string;
  returnDate: string;
  returnFromWarehouseId: number;
  returnFromWarehouseName: string;
  status: string;
  lineCount: number;
  totalReturnedQuantity: number;
}

export interface SupplierReturnNoteLineInput {
  goodsReceiptLineId: number;
  returnedQuantity: number;
  lineNotes: string | null;
}

export interface CreateSupplierReturnNoteRequest {
  goodsReceiptNoteId: number;
  returnFromWarehouseId: number;
  returnDate: string;
  vehicleNumber: string | null;
  reason: string | null;
  notes: string | null;
  lines: SupplierReturnNoteLineInput[];
}

export interface UpdateSupplierReturnNoteRequest {
  returnFromWarehouseId: number;
  returnDate: string;
  vehicleNumber: string | null;
  reason: string | null;
  notes: string | null;
  lines: SupplierReturnNoteLineInput[];
}
