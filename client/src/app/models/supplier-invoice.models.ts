// ─── Supplier Invoice ─────────────────────────────────────────────────────

export const SINV_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Partially Paid', value: 'PartiallyPaid' },
  { label: 'Paid', value: 'Paid' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface SupplierInvoiceLineDto {
  id: number;
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  unitOfMeasureCode: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface SupplierInvoiceDto {
  id: number;
  code: string;
  supplierId: number;
  supplierCode: string;
  supplierName: string;
  purchaseOrderId: number;
  purchaseOrderCode: string;
  supplierInvoiceNumber: string | null;
  invoiceDate: string;                  // DateOnly
  dueDate: string;
  status: string;
  currencyId: number;
  currencyCode: string;
  currencySymbol: string;
  exchangeRate: number;
  vatRate: number;
  subtotalAmount: number;
  vatAmount: number;
  totalAmount: number;                  // gross
  amountPaid: number;
  amountDue: number;
  baseTotalAmount: number;              // BDT
  approvedAt: string | null;
  approvedBy: string | null;
  notes: string | null;
  lines: SupplierInvoiceLineDto[];
}

export interface SupplierInvoiceListItemDto {
  id: number;
  code: string;
  supplierId: number;
  supplierName: string;
  purchaseOrderId: number;
  purchaseOrderCode: string;
  supplierInvoiceNumber: string | null;
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
}

export interface SupplierInvoiceLineInput {
  rawMaterialId: number;
  quantity: number;
  unitPrice: number;
  lineNotes: string | null;
}

export interface CreateSupplierInvoiceRequest {
  purchaseOrderId: number;
  supplierInvoiceNumber: string | null;
  vatRate: number;
  invoiceDate: string;
  dueDate: string | null;
  notes: string | null;
  lines: SupplierInvoiceLineInput[];
  isOpening?: boolean;   // Phase A1 — go-live opening bill (no GL on approve)
}

export interface UpdateSupplierInvoiceRequest {
  supplierInvoiceNumber: string | null;
  vatRate: number;
  invoiceDate: string;
  dueDate: string;
  notes: string | null;
  lines: SupplierInvoiceLineInput[];
}
