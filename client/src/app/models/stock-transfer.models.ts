// ─── Stock Transfer ───────────────────────────────────────────────────────

export const STOCK_TRANSFER_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft',  value: 'Draft' },
  { label: 'Posted', value: 'Posted' }
];

export interface StockTransferLineDto {
  id: number;
  itemType: string;                  // "RawMaterial" | "Product"
  rawMaterialId: number | null;
  productId: number | null;
  itemCode: string;
  itemName: string;
  unitOfMeasureCode: string;
  quantity: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface StockTransferDto {
  id: number;
  code: string;
  sourceWarehouseId: number;
  sourceWarehouseCode: string;
  sourceWarehouseName: string;
  destinationWarehouseId: number;
  destinationWarehouseCode: string;
  destinationWarehouseName: string;
  transferDate: string;              // DateOnly
  status: string;
  postedAt: string | null;
  postedBy: string | null;
  notes: string | null;
  lines: StockTransferLineDto[];
}

export interface StockTransferListItemDto {
  id: number;
  code: string;
  sourceWarehouseId: number;
  sourceWarehouseName: string;
  destinationWarehouseId: number;
  destinationWarehouseName: string;
  transferDate: string;
  status: string;
  lineCount: number;
  totalQuantity: number;
}

export interface StockTransferLineInput {
  rawMaterialId: number | null;
  productId: number | null;
  quantity: number;
  lineNotes: string | null;
}

export interface CreateStockTransferRequest {
  sourceWarehouseId: number;
  destinationWarehouseId: number;
  transferDate: string;
  notes: string | null;
  lines: StockTransferLineInput[];
}

export interface UpdateStockTransferRequest {
  sourceWarehouseId: number;
  destinationWarehouseId: number;
  transferDate: string;
  notes: string | null;
  lines: StockTransferLineInput[];
}
