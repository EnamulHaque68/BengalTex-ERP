// ─── Bill of Materials ────────────────────────────────────────────────────

export const BOM_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Archived', value: 'Archived' }
];

export interface BomLineDto {
  id: number;
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  unitOfMeasureCode: string;
  quantity: number;
  wastagePercent: number;
  effectiveQuantity: number;
  standardCost: number;
  lineCost: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface BomDto {
  id: number;
  code: string;
  productId: number;
  productCode: string;
  productName: string;
  productUnitOfMeasureCode: string;
  version: number;
  name: string | null;
  outputQuantity: number;
  status: string;
  isActive: boolean;
  effectiveDate: string | null;
  approvedAt: string | null;
  approvedBy: string | null;
  notes: string | null;
  totalMaterialCost: number;
  costPerUnit: number;
  lines: BomLineDto[];
}

export interface BomListItemDto {
  id: number;
  code: string;
  productId: number;
  productName: string;
  version: number;
  status: string;
  isActive: boolean;
  outputQuantity: number;
  lineCount: number;
  totalMaterialCost: number;
}

export interface BomLineInput {
  rawMaterialId: number;
  quantity: number;
  wastagePercent: number;
  lineNotes: string | null;
}

export interface CreateBomRequest {
  productId: number;
  name: string | null;
  outputQuantity: number;
  effectiveDate: string | null;
  notes: string | null;
  lines: BomLineInput[];
}

export interface UpdateBomRequest {
  name: string | null;
  outputQuantity: number;
  effectiveDate: string | null;
  notes: string | null;
  lines: BomLineInput[];
}
