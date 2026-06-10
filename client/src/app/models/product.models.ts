// ─── Product Category ─────────────────────────────────────────────────────

export interface ProductCategoryDto {
  id: number;
  code: string;
  name: string;
  description: string | null;
  productCount: number;
  isActive: boolean;
}

export interface CreateProductCategoryRequest {
  code: string;
  name: string;
  description: string | null;
}

export interface UpdateProductCategoryRequest {
  name: string;
  description: string | null;
  isActive: boolean;
}

// ─── Product ──────────────────────────────────────────────────────────────

export interface ProductDto {
  id: number;
  code: string;
  name: string;
  specification: string | null;
  productCategoryId: number;
  productCategoryName: string;
  unitOfMeasureId: number;
  unitOfMeasureCode: string;
  size: string | null;
  color: string | null;
  material: string | null;
  hsCode: string | null;
  salesPrice: number;
  reorderLevel: number;
  weightedAverageCost: number;        // system-maintained production cost (Phase 14)
  isStockItem: boolean;
  imageUrl: string | null;
  notes: string | null;
  isActive: boolean;
}

export interface ProductListItemDto {
  id: number;
  code: string;
  name: string;
  productCategoryName: string;
  unitOfMeasureCode: string;
  salesPrice: number;
  reorderLevel: number;
  weightedAverageCost: number;
  isStockItem: boolean;
  isActive: boolean;
}

export interface CreateProductRequest {
  code: string | null;
  name: string;
  specification: string | null;
  productCategoryId: number;
  unitOfMeasureId: number;
  size: string | null;
  color: string | null;
  material: string | null;
  hsCode: string | null;
  salesPrice: number;
  reorderLevel: number;
  isStockItem: boolean;
  imageUrl: string | null;
  notes: string | null;
}

export interface UpdateProductRequest {
  name: string;
  specification: string | null;
  productCategoryId: number;
  unitOfMeasureId: number;
  size: string | null;
  color: string | null;
  material: string | null;
  hsCode: string | null;
  salesPrice: number;
  reorderLevel: number;
  isStockItem: boolean;
  imageUrl: string | null;
  notes: string | null;
  isActive: boolean;
}
