// ─── Raw Material ─────────────────────────────────────────────────────────

export const MATERIAL_CATEGORIES: { label: string; value: string }[] = [
  { label: 'Yarn', value: 'Yarn' },
  { label: 'Fabric', value: 'Fabric' },
  { label: 'Ink', value: 'Ink' },
  { label: 'Chemical', value: 'Chemical' },
  { label: 'Thread', value: 'Thread' },
  { label: 'Paper / Board', value: 'PaperBoard' },
  { label: 'Packaging', value: 'Packaging' },
  { label: 'Adhesive', value: 'Adhesive' },
  { label: 'Other', value: 'Other' }
];

export interface RawMaterialDto {
  id: number;
  code: string;
  name: string;
  specification: string | null;
  category: string;
  unitOfMeasureId: number;
  unitOfMeasureCode: string;
  minimumStockLevel: number;
  openingStock: number;
  standardCost: number;
  preferredSupplierId: number | null;
  preferredSupplierName: string | null;
  notes: string | null;
  isActive: boolean;
}

export interface RawMaterialListItemDto {
  id: number;
  code: string;
  name: string;
  category: string;
  unitOfMeasureCode: string;
  minimumStockLevel: number;
  standardCost: number;
  preferredSupplierName: string | null;
  isActive: boolean;
}

export interface CreateRawMaterialRequest {
  code: string | null;
  name: string;
  specification: string | null;
  category: string;
  unitOfMeasureId: number;
  minimumStockLevel: number;
  openingStock: number;
  standardCost: number;
  preferredSupplierId: number | null;
  notes: string | null;
}

export interface UpdateRawMaterialRequest {
  name: string;
  specification: string | null;
  category: string;
  unitOfMeasureId: number;
  minimumStockLevel: number;
  openingStock: number;
  standardCost: number;
  preferredSupplierId: number | null;
  notes: string | null;
  isActive: boolean;
}
