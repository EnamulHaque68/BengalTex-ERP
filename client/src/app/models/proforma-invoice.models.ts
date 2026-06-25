// ─── Proforma Invoice ───────────────────────────────────────────────────

export const PFM_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Sent', value: 'Sent' },
  { label: 'Accepted', value: 'Accepted' },
  { label: 'Expired', value: 'Expired' },
  { label: 'Cancelled', value: 'Cancelled' },
  { label: 'Converted', value: 'Converted' }
];

export interface ProformaInvoiceLineDto {
  id: number;
  productId: number;
  productCode: string;
  productName: string;
  productUnit: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  sortOrder: number;
  lineNotes: string | null;
}

export interface ProformaInvoiceDto {
  id: number;
  code: string;
  customerId: number;
  customerName: string;
  salesOrderId: number | null;
  salesOrderCode: string | null;
  issueDate: string;
  validUntil: string;
  status: string;
  currencyId: number;
  currencyCode: string;
  exchangeRate: number;
  vatRate: number;
  subtotalAmount: number;
  vatAmount: number;
  totalAmount: number;
  sentAt: string | null;
  sentBy: string | null;
  acceptedAt: string | null;
  expiredAt: string | null;
  convertedCustomerInvoiceId: number | null;
  convertedCustomerInvoiceCode: string | null;
  quotationId: number | null;
  quotationCode: string | null;
  convertedSalesOrderId: number | null;
  convertedSalesOrderCode: string | null;
  customerConfirmationType: string | null;
  customerConfirmationReference: string | null;
  customerConfirmationDate: string | null;
  hasConfirmationAttachment: boolean;
  notes: string | null;
  lines: ProformaInvoiceLineDto[];
}

export const CONFIRMATION_TYPES: { label: string; value: string }[] = [
  { label: 'Purchase Order (PO)', value: 'PurchaseOrder' },
  { label: 'Letter of Credit (LC)', value: 'LetterOfCredit' },
  { label: 'Advance Payment', value: 'AdvancePayment' },
  { label: 'Signed Proforma', value: 'SignedProforma' },
  { label: 'Email Approval', value: 'EmailApproval' }
];

export interface ConvertProformaToSoRequest {
  customerConfirmationType: string;
  customerConfirmationReference: string | null;
  customerConfirmationDate: string | null;
  customerConfirmationAttachment: string | null;
}

export interface ProformaInvoiceLineInput {
  productId: number;
  quantity: number;
  unitPrice: number;
  lineNotes: string | null;
}

export interface CreateProformaInvoiceRequest {
  customerId: number;
  salesOrderId: number | null;
  issueDate: string;
  validUntil: string;
  currencyId: number;
  exchangeRate: number;
  vatRate: number;
  notes: string | null;
  lines: ProformaInvoiceLineInput[];
}

export interface UpdateProformaInvoiceRequest {
  issueDate: string;
  validUntil: string;
  vatRate: number;
  notes: string | null;
  lines: ProformaInvoiceLineInput[];
}

export interface ConvertProformaRequest {
  salesOrderId: number;
  invoiceDate: string;
  dueDate: string | null;
}
