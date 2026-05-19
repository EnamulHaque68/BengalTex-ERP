// ─── VAT Challan ──────────────────────────────────────────────────────────

export interface VatChallanDto {
  id: number;
  code: string;
  customerInvoiceId: number;
  customerInvoiceCode: string;
  customerId: number;
  customerName: string;
  challanDate: string;                  // DateOnly
  subtotalAmount: number;
  vatAmount: number;
  totalAmount: number;
  notes: string | null;
}

export interface VatChallanListItemDto {
  id: number;
  code: string;
  customerInvoiceId: number;
  customerInvoiceCode: string;
  customerId: number;
  customerName: string;
  challanDate: string;
  subtotalAmount: number;
  vatAmount: number;
  totalAmount: number;
}
