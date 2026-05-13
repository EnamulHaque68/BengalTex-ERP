// ─── Currency ─────────────────────────────────────────────────────────────

export interface CurrencyDto {
  id: number;
  code: string;
  name: string;
  symbol: string;
  exchangeRateToBase: number;
  isBaseCurrency: boolean;
  isActive: boolean;
}

export interface CreateCurrencyRequest {
  code: string;
  name: string;
  symbol: string;
  exchangeRateToBase: number;
  isBaseCurrency: boolean;
}

export interface UpdateCurrencyRequest {
  name: string;
  symbol: string;
  exchangeRateToBase: number;
  isBaseCurrency: boolean;
  isActive: boolean;
}

// ─── Unit of Measure ──────────────────────────────────────────────────────

export type UnitTypeName = 'Count' | 'Weight' | 'Length' | 'Volume' | 'Area';

export const UNIT_TYPES: UnitTypeName[] = ['Count', 'Weight', 'Length', 'Volume', 'Area'];

export interface UnitOfMeasureDto {
  id: number;
  code: string;
  name: string;
  symbol: string;
  unitType: UnitTypeName;
  baseUnitId: number | null;
  baseUnitCode: string | null;
  conversionFactor: number;
  isActive: boolean;
}

export interface CreateUnitOfMeasureRequest {
  code: string;
  name: string;
  symbol: string;
  unitType: UnitTypeName;
  baseUnitId: number | null;
  conversionFactor: number;
}

export interface UpdateUnitOfMeasureRequest {
  name: string;
  symbol: string;
  conversionFactor: number;
  isActive: boolean;
}

// ─── Warehouse ────────────────────────────────────────────────────────────

export type WarehouseTypeName =
  | 'General' | 'RawMaterial' | 'FinishedGoods' | 'WorkInProgress' | 'Reject';

export const WAREHOUSE_TYPES: WarehouseTypeName[] = [
  'General', 'RawMaterial', 'FinishedGoods', 'WorkInProgress', 'Reject'
];

export interface WarehouseDto {
  id: number;
  code: string;
  name: string;
  warehouseType: WarehouseTypeName;
  address: string | null;
  factoryId: number;
  factoryName: string | null;
  isActive: boolean;
}

export interface CreateWarehouseRequest {
  code: string;
  name: string;
  warehouseType: WarehouseTypeName;
  address: string | null;
  factoryId: number;
}

export interface UpdateWarehouseRequest {
  name: string;
  warehouseType: WarehouseTypeName;
  address: string | null;
  isActive: boolean;
}
