// ─── Customer Invoice ─────────────────────────────────────────────────────

export const CINV_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Issued', value: 'Issued' },
  { label: 'Partially Paid', value: 'PartiallyPaid' },
  { label: 'Paid', value: 'Paid' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface CustomerInvoiceLineDto {
  id: number;
  productId: number;
  productCode: string;
  productName: string;
  unitOfMeasureCode: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface CustomerInvoiceDto {
  id: number;
  code: string;
  customerId: number;
  customerCode: string;
  customerName: string;
  salesOrderId: number;
  salesOrderCode: string;
  invoiceDate: string;                  // DateOnly
  dueDate: string;
  status: string;
  currencyId: number;
  currencyCode: string;
  currencySymbol: string;
  exchangeRate: number;
  vatRate: number;                      // 0.15 = 15%
  subtotalAmount: number;               // net of VAT (document currency)
  vatAmount: number;
  totalAmount: number;                  // gross
  amountPaid: number;
  amountDue: number;
  baseTotalAmount: number;              // BDT
  issuedAt: string | null;
  issuedBy: string | null;
  notes: string | null;
  vatChallanCode: string | null;        // populated when auto-issued
  epbFormNumber: string | null;
  lcNumber: string | null;
  shipmentDate: string | null;
  lines: CustomerInvoiceLineDto[];
}

export interface CustomerInvoiceListItemDto {
  id: number;
  code: string;
  customerId: number;
  customerName: string;
  salesOrderId: number;
  salesOrderCode: string;
  invoiceDate: string;
  dueDate: string;
  status: string;
  currencyCode: string;
  exchangeRate: number;
  vatRate: number;
  subtotalAmount: number;
  vatAmount: number;
  totalAmount: number;
  amountPaid: number;
  amountDue: number;
  baseTotalAmount: number;              // BDT
  lineCount: number;
  epbFormNumber: string | null;
  shipmentDate: string | null;
}

export interface MarkExportedRequest {
  epbFormNumber: string | null;
  lcNumber: string | null;
  shipmentDate: string | null;
}

export interface CustomerInvoiceLineInput {
  productId: number;
  quantity: number;
  unitPrice: number;
  lineNotes: string | null;
}

export interface CreateCustomerInvoiceRequest {
  salesOrderId: number;
  vatRate: number;
  invoiceDate: string;
  dueDate: string | null;
  notes: string | null;
  lines: CustomerInvoiceLineInput[];
}

export interface UpdateCustomerInvoiceRequest {
  vatRate: number;
  invoiceDate: string;
  dueDate: string;
  notes: string | null;
  lines: CustomerInvoiceLineInput[];
}
