// ─── Fixed Assets ─────────────────────────────────────────────────────────

export const FIXED_ASSET_CATEGORIES: { label: string; value: string }[] = [
  { label: 'Machinery', value: 'Machinery' },
  { label: 'Vehicle', value: 'Vehicle' },
  { label: 'Office Equipment', value: 'OfficeEquipment' },
  { label: 'Furniture', value: 'Furniture' },
  { label: 'Computer', value: 'Computer' },
  { label: 'Building', value: 'Building' },
  { label: 'Other', value: 'Other' }
];

export const FIXED_ASSET_STATUSES: { label: string; value: string }[] = [
  { label: 'Active', value: 'Active' },
  { label: 'Disposed', value: 'Disposed' },
  { label: 'Written Off', value: 'WrittenOff' }
];

export interface FixedAssetDto {
  id: number;
  code: string;
  name: string;
  category: string;
  location: string | null;
  machineId: number | null;
  machineCode: string | null;
  acquisitionDate: string;
  acquisitionCost: number;
  salvageValue: number;
  usefulLifeYears: number;
  depreciationMethod: string;
  accumulatedDepreciation: number;
  netBookValue: number;
  monthlyDepreciation: number;
  lastDepreciationYearMonth: number | null;
  status: string;
  disposalDate: string | null;
  disposalProceeds: number | null;
  disposalNotes: string | null;
  disposedByUser: string | null;
  notes: string | null;
}

export interface SaveFixedAssetRequest {
  name: string;
  category: string;
  location: string | null;
  machineId: number | null;
  acquisitionDate: string;
  acquisitionCost: number;
  salvageValue: number;
  usefulLifeYears: number;
  notes: string | null;
}

export interface RunDepreciationRequest {
  year: number;
  month: number;
}

export interface DisposeFixedAssetRequest {
  disposalDate: string;
  disposalProceeds: number;
  notes: string | null;
  isWriteOff: boolean;
}

export interface AssetDepreciationRunLineDto {
  id: number;
  fixedAssetId: number;
  fixedAssetCode: string;
  fixedAssetName: string;
  monthlyDepreciation: number;
  accumulatedAfter: number;
  netBookValueAfter: number;
}

export interface AssetDepreciationRunDto {
  id: number;
  code: string;
  year: number;
  month: number;
  runDate: string;
  totalAmount: number;
  assetCount: number;
  postedByUser: string | null;
  notes: string | null;
  lines: AssetDepreciationRunLineDto[];
}
