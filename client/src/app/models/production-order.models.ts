// ─── Production Order ─────────────────────────────────────────────────────

export const PRODUCTION_ORDER_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'In Progress', value: 'InProgress' },
  { label: 'Completed', value: 'Completed' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export const PRODUCTION_STAGE_STATUSES: { label: string; value: string }[] = [
  { label: 'Pending', value: 'Pending' },
  { label: 'In Progress', value: 'InProgress' },
  { label: 'Completed', value: 'Completed' },
  { label: 'Skipped', value: 'Skipped' }
];

// Common garments routing stages — suggestions for the stage-name picker
export const COMMON_STAGE_NAMES: string[] = [
  'Cutting', 'Printing', 'Embroidery', 'Sewing', 'Finishing', 'Ironing', 'QC', 'Packing'
];

export interface ProductionPlannedLineDto {
  itemType: string;                 // "RawMaterial" | "Product" (sub-assembly)
  itemId: number;
  itemCode: string;
  itemName: string;
  unitOfMeasureCode: string;
  bomLineQuantity: number;
  wastagePercent: number;
  scaledQuantity: number;
  currentOnHand: number;
  sufficient: boolean;
}

export interface ProductionStageDto {
  id: number;
  sequence: number;
  stageName: string;
  status: string;
  plannedQuantity: number;
  completedQuantity: number;
  rejectedQuantity: number;
  productionLine: string | null;
  operatorEmployeeId: number | null;
  operatorEmployeeName: string | null;
  startedAt: string | null;
  completedAt: string | null;
  notes: string | null;
}

export interface ProductionStageInput {
  sequence: number;
  stageName: string;
  plannedQuantity: number | null;
  productionLine: string | null;
  operatorEmployeeId: number | null;
  notes: string | null;
}

export interface ProductionOrderDto {
  id: number;
  code: string;
  productId: number;
  productCode: string;
  productName: string;
  productUnitOfMeasureCode: string;
  bomId: number;
  bomCode: string;
  bomVersion: number;
  bomOutputQuantity: number;
  quantity: number;
  issueWarehouseId: number;
  issueWarehouseCode: string;
  issueWarehouseName: string;
  receiveWarehouseId: number;
  receiveWarehouseCode: string;
  receiveWarehouseName: string;
  plannedStartDate: string | null;
  plannedEndDate: string | null;
  actualStartDate: string | null;
  actualEndDate: string | null;
  status: string;
  completedAt: string | null;
  completedBy: string | null;
  notes: string | null;
  plannedLines: ProductionPlannedLineDto[];
  stages: ProductionStageDto[];
}

export interface ProductionOrderListItemDto {
  id: number;
  code: string;
  productId: number;
  productName: string;
  bomVersion: number;
  quantity: number;
  status: string;
  plannedStartDate: string | null;
  actualEndDate: string | null;
  stageCount: number;
  completedStageCount: number;
  currentStageName: string | null;
}

export interface CreateProductionOrderRequest {
  productId: number;
  bomId: number;
  quantity: number;
  issueWarehouseId: number;
  receiveWarehouseId: number;
  plannedStartDate: string | null;
  plannedEndDate: string | null;
  notes: string | null;
  stages?: ProductionStageInput[];
}

export interface UpdateProductionOrderRequest {
  productId: number;
  bomId: number;
  quantity: number;
  issueWarehouseId: number;
  receiveWarehouseId: number;
  plannedStartDate: string | null;
  plannedEndDate: string | null;
  notes: string | null;
  stages?: ProductionStageInput[];
}

export interface CompleteProductionStageRequest {
  completedQuantity: number;
  rejectedQuantity: number;
  notes: string | null;
}
