// ─── Stock Lots / Batch tracking ────────────────────────────────────────────

export const LOT_ITEM_TYPES: { label: string; value: string }[] = [
  { label: 'Raw Material', value: 'RawMaterial' },
  { label: 'Product', value: 'Product' }
];

export const LOT_STATUSES: { label: string; value: string }[] = [
  { label: 'Active', value: 'Active' },
  { label: 'Depleted', value: 'Depleted' },
  { label: 'Quarantined', value: 'Quarantined' },
  { label: 'Expired', value: 'Expired' }
];

export interface StockLotDto {
  id: number;
  code: string;
  lotNumber: string;
  itemType: string;                  // RawMaterial | Product
  itemId: number;
  itemCode: string;
  itemName: string;
  unitOfMeasureCode: string;
  warehouseId: number;
  warehouseName: string;
  supplierId: number | null;
  supplierName: string | null;
  shade: string | null;
  receivedDate: string;
  manufactureDate: string | null;
  expiryDate: string | null;
  initialQuantity: number;
  currentQuantity: number;
  status: string;
  isExpired: boolean;
  sourceType: string | null;
  sourceId: number | null;
  sourceCode: string | null;
  notes: string | null;
}

export interface StockLotMovementDto {
  id: number;
  code: string;
  movementType: string;
  signedQuantity: number;
  movementDate: string;
  referenceType: string | null;
  referenceCode: string | null;
  notes: string | null;
}

export interface StockLotDetailDto {
  lot: StockLotDto;
  movements: StockLotMovementDto[];
}
