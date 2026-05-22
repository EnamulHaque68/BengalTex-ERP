// ─── Buyer Style ──────────────────────────────────────────────────────────

export const STYLE_STATUSES: { label: string; value: string }[] = [
  { label: 'Development', value: 'Development' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Running', value: 'Running' },
  { label: 'Discontinued', value: 'Discontinued' }
];

export interface StyleDto {
  id: number;
  code: string;
  styleName: string;
  buyerId: number;
  buyerName: string;
  productId: number | null;
  productName: string | null;
  buyerStyleRef: string | null;
  season: string | null;
  status: string;
  description: string | null;
  notes: string | null;
  isActive: boolean;
}

export interface StyleListItemDto {
  id: number;
  code: string;
  styleName: string;
  buyerName: string;
  productName: string | null;
  season: string | null;
  status: string;
  isActive: boolean;
}

export interface CreateStyleRequest {
  code: string | null;
  styleName: string;
  buyerId: number;
  productId: number | null;
  buyerStyleRef: string | null;
  season: string | null;
  status: string;
  description: string | null;
  notes: string | null;
}

export interface UpdateStyleRequest {
  styleName: string;
  buyerId: number;
  productId: number | null;
  buyerStyleRef: string | null;
  season: string | null;
  status: string;
  description: string | null;
  notes: string | null;
  isActive: boolean;
}
