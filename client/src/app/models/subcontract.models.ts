// ─── Subcontracting ───────────────────────────────────────────────────────

export const SUBCONTRACT_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Issued', value: 'Issued' },
  { label: 'Received', value: 'Received' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface SubcontractLineDto {
  id: number;
  rawMaterialId: number | null;
  productId: number | null;
  itemType: string;            // "RawMaterial" | "Product"
  itemCode: string;
  itemName: string;
  uomCode: string;
  issuedQuantity: number;
  receivedQuantity: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface SubcontractOrderDto {
  id: number;
  code: string;
  subcontractorId: number;
  subcontractorName: string;
  orderDate: string;
  expectedReturnDate: string | null;
  processType: string;
  warehouseId: number;
  warehouseName: string;
  status: string;
  chargeAmount: number;
  issuedAt: string | null;
  issuedBy: string | null;
  receivedAt: string | null;
  receivedBy: string | null;
  notes: string | null;
  lines: SubcontractLineDto[];
}

export interface SubcontractOrderListItemDto {
  id: number;
  code: string;
  subcontractorName: string;
  orderDate: string;
  processType: string;
  warehouseName: string;
  status: string;
  lineCount: number;
  chargeAmount: number;
}

export interface SubcontractLineInput {
  rawMaterialId: number | null;
  productId: number | null;
  issuedQuantity: number;
  lineNotes: string | null;
}

export interface CreateSubcontractOrderRequest {
  subcontractorId: number;
  orderDate: string;
  expectedReturnDate: string | null;
  processType: string;
  warehouseId: number;
  chargeAmount: number;
  notes: string | null;
  lines: SubcontractLineInput[];
}

export interface UpdateSubcontractOrderRequest extends CreateSubcontractOrderRequest {}

export interface SubcontractReceiveLineInput {
  lineId: number;
  receivedQuantity: number;
}
