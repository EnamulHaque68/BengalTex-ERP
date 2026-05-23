// ─── Wastage Management ───────────────────────────────────────────────────

export interface WastageReasonDto {
  id: number;
  name: string;
  isReusable: boolean;
  isActive: boolean;
  description: string | null;
}

export interface SaveWastageReasonRequest {
  id?: number;
  name: string;
  isReusable: boolean;
  isActive?: boolean;
  description: string | null;
}

export interface WastageEntryDto {
  id: number;
  code: string;
  wastageDate: string;
  productionOrderId: number | null;
  productionOrderCode: string | null;
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  unitOfMeasureCode: string;
  wastageReasonId: number;
  wastageReasonName: string;
  isReusable: boolean;
  quantity: number;
  unitCost: number;
  totalCost: number;
  department: string | null;
  notes: string | null;
}

export interface WastageEntryListItemDto {
  id: number;
  code: string;
  wastageDate: string;
  rawMaterialName: string;
  wastageReasonName: string;
  isReusable: boolean;
  quantity: number;
  totalCost: number;
  department: string | null;
}

export interface SaveWastageEntryRequest {
  id?: number;
  wastageDate: string;
  productionOrderId: number | null;
  rawMaterialId: number | null;
  wastageReasonId: number | null;
  quantity: number;
  department: string | null;
  notes: string | null;
}

export interface WastageSummaryRowDto {
  wastageReasonId: number;
  wastageReasonName: string;
  isReusable: boolean;
  totalCost: number;
  count: number;
}

export interface WastageSummaryDto {
  fromDate: string;
  toDate: string;
  rows: WastageSummaryRowDto[];
  totalCost: number;
}
