// ─── Goods Receipt Note ───────────────────────────────────────────────────

export const GRN_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Posted', value: 'Posted' }
];

export interface GoodsReceiptLineDto {
  id: number;
  purchaseOrderLineId: number;
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  unitOfMeasureCode: string;
  orderedQuantity: number;            // snapshot from PO line
  receivedQuantity: number;           // this GRN line's quantity
  returnedQuantity: number;           // already-returned via posted SRNs
  sortOrder: number;
  lineNotes: string | null;
  lotNumber: string | null;           // optional lot/batch capture (Round 3 lot tracking)
  shade: string | null;
  manufactureDate: string | null;
  expiryDate: string | null;
}

export interface GoodsReceiptDto {
  id: number;
  code: string;
  purchaseOrderId: number;
  purchaseOrderCode: string;
  supplierId: number;
  supplierName: string;
  receiveDate: string;                // DateOnly → "YYYY-MM-DD"
  receivingWarehouseId: number;
  receivingWarehouseName: string;
  status: string;
  supplierDeliveryRef: string | null;
  postedAt: string | null;
  postedBy: string | null;
  notes: string | null;
  lines: GoodsReceiptLineDto[];
  // Optional linked import LC (Area B)
  letterOfCreditId: number | null;
  letterOfCreditCode: string | null;
  letterOfCreditNumber: string | null;
  letterOfCreditStatus: string | null;
}

export interface GoodsReceiptListItemDto {
  id: number;
  code: string;
  purchaseOrderId: number;
  purchaseOrderCode: string;
  supplierName: string;
  receiveDate: string;
  receivingWarehouseCode: string;
  status: string;
  lineCount: number;
}

export interface GoodsReceiptLineInput {
  purchaseOrderLineId: number;
  receivedQuantity: number;
  lineNotes: string | null;
  lotNumber?: string | null;
  shade?: string | null;
  manufactureDate?: string | null;
  expiryDate?: string | null;
}

export interface CreateGoodsReceiptRequest {
  purchaseOrderId: number;
  receiveDate: string;
  receivingWarehouseId: number;
  supplierDeliveryRef: string | null;
  notes: string | null;
  lines: GoodsReceiptLineInput[];
  letterOfCreditId?: number | null;   // optional; backend auto-links the PO's LC when omitted
}

export interface UpdateGoodsReceiptRequest {
  receiveDate: string;
  receivingWarehouseId: number;
  supplierDeliveryRef: string | null;
  notes: string | null;
  lines: GoodsReceiptLineInput[];
}
