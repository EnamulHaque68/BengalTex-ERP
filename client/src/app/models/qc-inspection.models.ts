// ─── QC Inspection ────────────────────────────────────────────────────────

export const QC_SOURCE_TYPES: { label: string; value: string }[] = [
  { label: 'Incoming Material (GRN)', value: 'IncomingMaterial' },
  { label: 'Finished Goods (Production)', value: 'FinishedGoods' }
];

export const QC_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft',  value: 'Draft' },
  { label: 'Posted', value: 'Posted' }
];

export const QC_RESULTS: { label: string; value: string }[] = [
  { label: 'Passed',          value: 'Passed' },
  { label: 'Partially Passed', value: 'PartiallyPassed' },
  { label: 'Failed',          value: 'Failed' }
];

export interface QcInspectionLineDto {
  id: number;
  itemType: string;                   // "RawMaterial" | "Product"
  rawMaterialId: number | null;
  productId: number | null;
  itemCode: string;
  itemName: string;
  unitOfMeasureCode: string;
  inspectedQuantity: number;
  passedQuantity: number;
  rejectedQuantity: number;
  availableAtSource: number;
  sortOrder: number;
  defectNotes: string | null;
}

export interface QcInspectionDto {
  id: number;
  code: string;
  sourceType: string;
  goodsReceiptNoteId: number | null;
  goodsReceiptNoteCode: string | null;
  productionOrderId: number | null;
  productionOrderCode: string | null;
  sourceLabel: string;
  inspectionDate: string;
  inspectedFromWarehouseId: number;
  inspectedFromWarehouseName: string;
  quarantineWarehouseId: number;
  quarantineWarehouseName: string;
  status: string;
  overallResult: string;
  inspectedBy: string | null;
  notes: string | null;
  lines: QcInspectionLineDto[];
}

export interface QcInspectionListItemDto {
  id: number;
  code: string;
  sourceType: string;
  sourceCode: string;
  inspectionDate: string;
  status: string;
  overallResult: string;
  lineCount: number;
  totalInspected: number;
  totalRejected: number;
}

export interface QcInspectionLineInput {
  rawMaterialId: number | null;
  productId: number | null;
  inspectedQuantity: number;
  passedQuantity: number;
  defectNotes: string | null;
}

export interface CreateQcInspectionRequest {
  sourceType: string;
  goodsReceiptNoteId: number | null;
  productionOrderId: number | null;
  inspectionDate: string;
  quarantineWarehouseId: number;
  inspectedBy: string | null;
  notes: string | null;
  lines: QcInspectionLineInput[];
}

export interface UpdateQcInspectionRequest {
  inspectionDate: string;
  quarantineWarehouseId: number;
  inspectedBy: string | null;
  notes: string | null;
  lines: QcInspectionLineInput[];
}
