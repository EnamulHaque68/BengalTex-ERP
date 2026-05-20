// ─── Quarantine Disposition ───────────────────────────────────────────────

export const DISPOSITION_TYPES: { label: string; value: string }[] = [
  { label: 'Release (back to usable)', value: 'Release' },
  { label: 'Scrap (write-off)',        value: 'Scrap' }
];

export const DISPOSITION_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft',  value: 'Draft' },
  { label: 'Posted', value: 'Posted' }
];

export interface QuarantineDispositionLineDto {
  id: number;
  itemType: string;                   // "RawMaterial" | "Product"
  rawMaterialId: number | null;
  productId: number | null;
  itemCode: string;
  itemName: string;
  unitOfMeasureCode: string;
  quantity: number;
  availableInQuarantine: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface QuarantineDispositionDto {
  id: number;
  code: string;
  dispositionType: string;
  dispositionDate: string;
  quarantineWarehouseId: number;
  quarantineWarehouseName: string;
  destinationWarehouseId: number | null;
  destinationWarehouseName: string | null;
  status: string;
  reason: string | null;
  postedAt: string | null;
  postedBy: string | null;
  notes: string | null;
  lines: QuarantineDispositionLineDto[];
}

export interface QuarantineDispositionListItemDto {
  id: number;
  code: string;
  dispositionType: string;
  dispositionDate: string;
  quarantineWarehouseId: number;
  quarantineWarehouseName: string;
  destinationWarehouseName: string | null;
  status: string;
  lineCount: number;
  totalQuantity: number;
}

export interface QuarantineDispositionLineInput {
  rawMaterialId: number | null;
  productId: number | null;
  quantity: number;
  lineNotes: string | null;
}

export interface CreateQuarantineDispositionRequest {
  dispositionType: string;
  dispositionDate: string;
  quarantineWarehouseId: number;
  destinationWarehouseId: number | null;
  reason: string | null;
  notes: string | null;
  lines: QuarantineDispositionLineInput[];
}

export interface UpdateQuarantineDispositionRequest {
  dispositionDate: string;
  destinationWarehouseId: number | null;
  reason: string | null;
  notes: string | null;
  lines: QuarantineDispositionLineInput[];
}
