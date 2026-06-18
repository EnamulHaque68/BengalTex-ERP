export const OPENING_STOCK_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Posted', value: 'Posted' }
];

export interface OpeningStockLineInput {
  itemType: string;            // "RawMaterial" | "Product"
  itemId: number;
  quantity: number;
  unitCost: number;
  lineNotes: string | null;
}

export interface OpeningStockLineDto {
  id: number;
  itemType: string;
  rawMaterialId: number | null;
  productId: number | null;
  itemCode: string;
  itemName: string;
  unitOfMeasureCode: string;
  quantity: number;
  unitCost: number;
  lineValue: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface OpeningStockEntryDto {
  id: number;
  code: string;
  entryDate: string;
  warehouseId: number;
  warehouseName: string;
  status: string;
  postedAt: string | null;
  postedBy: string | null;
  notes: string | null;
  totalValue: number;
  lines: OpeningStockLineDto[];
}

export interface OpeningStockListItemDto {
  id: number;
  code: string;
  entryDate: string;
  warehouseCode: string;
  status: string;
  lineCount: number;
  totalValue: number;
}

export interface SaveOpeningStockRequest {
  entryDate: string;
  warehouseId: number;
  notes: string | null;
  lines: OpeningStockLineInput[];
}
