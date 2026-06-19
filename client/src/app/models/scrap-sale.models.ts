// ─── Scrap / Wastage Sale ───────────────────────────────────────────────────

export const SCRAP_SALE_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Posted', value: 'Posted' }
];

export const SCRAP_PAYMENT_METHODS: { label: string; value: string }[] = [
  { label: 'Cash', value: 'Cash' },
  { label: 'Bank Transfer', value: 'BankTransfer' },
  { label: 'Cheque', value: 'Cheque' },
  { label: 'Mobile Banking (bKash / Nagad)', value: 'MobileBanking' },
  { label: 'Other', value: 'Other' }
];

export interface ScrapSaleLineInput {
  description: string;
  quantity: number;
  unit: string | null;
  unitPrice: number;
}

export interface ScrapSaleLineDto {
  id: number;
  description: string;
  quantity: number;
  unit: string | null;
  unitPrice: number;
  lineTotal: number;
  sortOrder: number;
}

export interface ScrapSaleDto {
  id: number;
  code: string;
  saleDate: string;
  buyerName: string | null;
  paymentMethod: string;
  status: string;
  postedAt: string | null;
  postedBy: string | null;
  notes: string | null;
  totalAmount: number;
  lines: ScrapSaleLineDto[];
}

export interface ScrapSaleListItemDto {
  id: number;
  code: string;
  saleDate: string;
  buyerName: string | null;
  paymentMethod: string;
  status: string;
  lineCount: number;
  totalAmount: number;
}

export interface SaveScrapSaleRequest {
  saleDate: string;
  buyerName: string | null;
  paymentMethod: string;
  notes: string | null;
  lines: ScrapSaleLineInput[];
}
