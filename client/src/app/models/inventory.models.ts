// ─── Inventory — Stock On Hand, Movements, Adjustments ───────────────────

export const MOVEMENT_TYPES: { label: string; value: string }[] = [
  { label: 'Opening Stock', value: 'OpeningStock' },
  { label: 'GRN Receipt', value: 'GrnReceipt' },
  { label: 'Adjustment In', value: 'AdjustmentIn' },
  { label: 'Adjustment Out', value: 'AdjustmentOut' },
  { label: 'Production Issue', value: 'ProductionIssue' },
  { label: 'Production Receipt', value: 'ProductionReceipt' },
  { label: 'Sales Dispatch', value: 'SalesDispatch' },
  { label: 'Transfer In', value: 'TransferIn' },
  { label: 'Transfer Out', value: 'TransferOut' }
];

export const STOCK_ADJUSTMENT_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Posted', value: 'Posted' }
];

// ─── Stock On Hand ────────────────────────────────────────────────────────

export interface StockOnHandDto {
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  unitOfMeasureCode: string;
  warehouseId: number;
  warehouseCode: string;
  warehouseName: string;
  quantity: number;
  minimumStockLevel: number;
  belowMinimum: boolean;
}

// ─── Stock Movement ───────────────────────────────────────────────────────

export interface StockMovementDto {
  id: number;
  code: string;
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  unitOfMeasureCode: string;
  warehouseId: number;
  warehouseCode: string;
  signedQuantity: number;             // + in, − out
  movementType: string;
  referenceType: string | null;
  referenceId: number | null;
  referenceCode: string | null;
  movementDate: string;
  notes: string | null;
  createdAt: string;
}

// ─── Stock Adjustment ─────────────────────────────────────────────────────

export interface StockAdjustmentLineDto {
  id: number;
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  unitOfMeasureCode: string;
  signedQuantity: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface StockAdjustmentDto {
  id: number;
  code: string;
  adjustmentDate: string;
  warehouseId: number;
  warehouseCode: string;
  warehouseName: string;
  reason: string;
  status: string;
  postedAt: string | null;
  postedBy: string | null;
  notes: string | null;
  lines: StockAdjustmentLineDto[];
}

export interface StockAdjustmentListItemDto {
  id: number;
  code: string;
  adjustmentDate: string;
  warehouseId: number;
  warehouseName: string;
  reason: string;
  status: string;
  lineCount: number;
}

export interface StockAdjustmentLineInput {
  rawMaterialId: number;
  signedQuantity: number;
  lineNotes: string | null;
}

export interface CreateStockAdjustmentRequest {
  adjustmentDate: string;
  warehouseId: number;
  reason: string;
  notes: string | null;
  lines: StockAdjustmentLineInput[];
}

export interface UpdateStockAdjustmentRequest {
  adjustmentDate: string;
  warehouseId: number;
  reason: string;
  notes: string | null;
  lines: StockAdjustmentLineInput[];
}
