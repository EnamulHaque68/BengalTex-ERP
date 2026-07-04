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
  hsCode: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  sortOrder: number;
  lineNotes: string | null;
  salesOrderLineId: number | null;       // originating SO line (traceability + edit coverage)
  // Per-line export packing
  cartonNumberFrom: number | null;
  cartonNumberTo: number | null;
  unitsPerCarton: number | null;
  netWeightKgPerLine: number | null;
  grossWeightKgPerLine: number | null;
}

export interface InvoiceLinePackingInput {
  lineId: number;
  cartonNumberFrom: number | null;
  cartonNumberTo: number | null;
  unitsPerCarton: number | null;
  netWeightKgPerLine: number | null;
  grossWeightKgPerLine: number | null;
}

export interface SetLinesPackingRequest {
  lines: InvoiceLinePackingInput[];
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
  // Export shipping fields (Commercial Invoice / Packing List)
  incoTerm: string | null;
  portOfLoading: string | null;
  portOfDischarge: string | null;
  vesselName: string | null;
  countryOfDestination: string | null;
  shippingMarks: string | null;
  totalPackages: number | null;
  grossWeightKg: number | null;
  netWeightKg: number | null;
  containerNumber: string | null;
  sealNumber: string | null;
  truckNumber: string | null;
  beneficiaryBankAccountId: number | null;
  beneficiaryBank: BeneficiaryBankDto | null;
  lines: CustomerInvoiceLineDto[];
}

export interface BeneficiaryBankDto {
  id: number;
  accountName: string;
  bankName: string;
  branchName: string | null;
  accountNumber: string;
  routingNumber: string | null;
  swiftCode: string | null;
  currency: string;
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
  customerIsExport: boolean;
}

export interface MarkExportedRequest {
  epbFormNumber: string | null;
  lcNumber: string | null;
  shipmentDate: string | null;
  incoTerm: string | null;
  portOfLoading: string | null;
  portOfDischarge: string | null;
  vesselName: string | null;
  countryOfDestination: string | null;
  shippingMarks: string | null;
  totalPackages: number | null;
  grossWeightKg: number | null;
  netWeightKg: number | null;
  containerNumber: string | null;
  sealNumber: string | null;
  truckNumber: string | null;
  beneficiaryBankAccountId: number | null;
}

export interface CustomerInvoiceLineInput {
  productId: number;
  quantity: number;
  unitPrice: number;
  lineNotes: string | null;
  salesOrderLineId: number | null;       // links the line to its SO line (drives invoice coverage)
}

export interface CreateCustomerInvoiceRequest {
  salesOrderId: number;
  vatRate: number;
  invoiceDate: string;
  dueDate: string | null;
  notes: string | null;
  lines: CustomerInvoiceLineInput[];
  isOpening?: boolean;   // Phase A1 — go-live opening invoice (no GL / challan on issue)
}

export interface UpdateCustomerInvoiceRequest {
  vatRate: number;
  invoiceDate: string;
  dueDate: string;
  notes: string | null;
  lines: CustomerInvoiceLineInput[];
}
