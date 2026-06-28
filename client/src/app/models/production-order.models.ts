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
  workCenterId?: number | null;        // Phase 4
  workCenterName?: string | null;
  shiftId?: number | null;
  shiftName?: string | null;
}

export interface ProductionStageInput {
  sequence: number;
  stageName: string;
  plannedQuantity: number | null;
  productionLine: string | null;
  operatorEmployeeId: number | null;
  notes: string | null;
  workCenterId?: number | null;        // Phase 4
  shiftId?: number | null;
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
  // Phase 1 — source Sales Order link (null = standalone / manual run)
  salesOrderId: number | null;
  salesOrderCode: string | null;
  salesOrderLineId: number | null;
  customerName: string | null;
  // Phase 5 — Quality Hold
  requiresQc?: boolean;
  qcReleasedAt?: string | null;
  qcHeld?: boolean;            // qcHeldQuantity > 0
  qcHeldQuantity?: number;     // remaining QC-held qty
  // Phase 6 — cost sheet (base BDT)
  materialCost?: number;       // auto at Complete
  labourCost?: number;
  machineCost?: number;
  overheadCost?: number;
  subcontractCost?: number;
  wastageCost?: number;
  rejectCost?: number;
  totalProductionCost?: number;
  costPerUnit?: number;
}

export interface UpdateProductionCostsRequest {
  labourCost: number;
  machineCost: number;
  overheadCost: number;
  subcontractCost: number;
  wastageCost: number;
  rejectCost: number;
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
  salesOrderId: number | null;       // Phase 1 — source SO (null = standalone)
  salesOrderCode: string | null;
  requiresQc?: boolean;              // Phase 5
  qcHeld?: boolean;                  // qcHeldQuantity > 0
  qcHeldQuantity?: number;           // remaining QC-held qty
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
  salesOrderId?: number | null;      // Phase 1
  salesOrderLineId?: number | null;
  requiresQc?: boolean;              // Phase 5
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
  salesOrderId?: number | null;      // Phase 1
  salesOrderLineId?: number | null;
  requiresQc?: boolean;              // Phase 5
}

export interface CompleteProductionStageRequest {
  completedQuantity: number;
  rejectedQuantity: number;
  notes: string | null;
}

// Phase 5b — completed productions still QC-held (for the QC inspection picker)
export interface ProductionAwaitingQcDto {
  id: number;
  code: string;
  productId: number;
  productCode: string;
  productName: string;
  unitOfMeasureCode: string;
  totalQuantity: number;
  remainingQcQuantity: number;
}

// Phase 7 — end-to-end traceability
export interface TraceConsumedItemDto {
  itemType: string;
  itemCode: string;
  itemName: string;
  unitOfMeasureCode: string;
  quantity: number;
}
export interface TraceJobCardDto {
  code: string;
  status: string;
  batchNumber: string | null;
  operatorName: string | null;
  machineName: string | null;
  quantity: number;
  completedQuantity: number;
  rejectedQuantity: number;
}
export interface TraceLotDto {
  code: string;
  lotNumber: string;
  shade: string | null;
  currentQuantity: number;
}
export interface ProductionTraceabilityDto {
  productionOrderId: number;
  code: string;
  status: string;
  productCode: string;
  productName: string;
  quantity: number;
  actualStartDate: string | null;
  actualEndDate: string | null;
  bomVersion: number;
  bomCode: string;
  salesOrderCode: string | null;
  customerName: string | null;
  quotationCode: string | null;
  consumedItems: TraceConsumedItemDto[];
  jobCards: TraceJobCardDto[];
  lots: TraceLotDto[];
}

// ─── Phase 8 — Manufacturing Calendar ───────────────────────────────────────
export interface ProductionCalendarHolidayDto {
  date: string;            // "YYYY-MM-DD"
  name: string;
}

export interface ProductionCalendarEventDto {
  id: number;
  code: string;
  productId: number;
  productName: string;
  quantity: number;
  status: string;          // Draft | InProgress | Completed | Cancelled
  plannedStartDate: string | null;
  plannedEndDate: string | null;
  actualStartDate: string | null;
  actualEndDate: string | null;
  salesOrderId: number | null;
  salesOrderCode: string | null;
}

export interface ProductionCalendarDto {
  from: string;
  to: string;
  weekendDays: number[];   // DayOfWeek ints (0=Sun … 6=Sat)
  holidays: ProductionCalendarHolidayDto[];
  orders: ProductionCalendarEventDto[];
}
