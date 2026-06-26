// ─── MRP — Material Requirement Planning (Phase 3) ─────────────────────────

export interface MrpItemDto {
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  unitOfMeasureCode: string;
  requiredQuantity: number;        // = reserved (firm open-production demand)
  onHandQuantity: number;
  availableQuantity: number;       // onHand − required
  incomingQuantity: number;        // open purchase orders, ordered − received
  shortageQuantity: number;        // max(0, required − onHand − incoming)
  estimatedUnitPrice: number;      // raw material weighted-average cost
  minimumStockLevel: number;
}

export interface MrpResultDto {
  items: MrpItemDto[];
  shortageCount: number;
  totalEstimatedShortageCost: number;
}

export interface GenerateMrpRequisitionRequest {
  rawMaterialIds: number[];
}
