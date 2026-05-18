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
  totalAmount: number;
  amountPaid: number;
  amountDue: number;
  issuedAt: string | null;
  issuedBy: string | null;
  notes: string | null;
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
  totalAmount: number;
  amountPaid: number;
  amountDue: number;
  lineCount: number;
}

export interface CustomerInvoiceLineInput {
  productId: number;
  quantity: number;
  unitPrice: number;
  lineNotes: string | null;
}

export interface CreateCustomerInvoiceRequest {
  salesOrderId: number;
  invoiceDate: string;
  dueDate: string | null;
  notes: string | null;
  lines: CustomerInvoiceLineInput[];
}

export interface UpdateCustomerInvoiceRequest {
  invoiceDate: string;
  dueDate: string;
  notes: string | null;
  lines: CustomerInvoiceLineInput[];
}
