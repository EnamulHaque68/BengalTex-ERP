// ─── Product Variants (catalog SKU breakdown) ───────────────────────────────

export interface ProductVariantDto {
  id: number;
  productId: number;
  variantCode: string;
  name: string | null;
  color: string | null;
  size: string | null;
  sku: string | null;
  salesPriceOverride: number | null;
  effectiveSalesPrice: number;
  notes: string | null;
  isActive: boolean;
}

export interface CreateProductVariantRequest {
  productId: number;
  variantCode: string;
  name: string | null;
  color: string | null;
  size: string | null;
  sku: string | null;
  salesPriceOverride: number | null;
  notes: string | null;
  isActive: boolean;
}

export interface UpdateProductVariantRequest {
  id: number;
  variantCode: string;
  name: string | null;
  color: string | null;
  size: string | null;
  sku: string | null;
  salesPriceOverride: number | null;
  notes: string | null;
  isActive: boolean;
}

export interface BulkCreateProductVariantsRequest {
  productId: number;
  colors: string[];
  sizes: string[];
  skuPrefix: string | null;
}
