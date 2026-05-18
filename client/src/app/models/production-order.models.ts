// ─── Production Order ─────────────────────────────────────────────────────

export const PRODUCTION_ORDER_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'In Progress', value: 'InProgress' },
  { label: 'Completed', value: 'Completed' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface ProductionPlannedLineDto {
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  unitOfMeasureCode: string;
  bomLineQuantity: number;
  wastagePercent: number;
  scaledQuantity: number;
  currentOnHand: number;
  sufficient: boolean;
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
}
