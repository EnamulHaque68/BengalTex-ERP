// ─── Email gateway ────────────────────────────────────────────────────────

export const EMAIL_STATUSES: { label: string; value: string }[] = [
  { label: 'Sent', value: 'Sent' },
  { label: 'Failed', value: 'Failed' }
];

export const EMAIL_SOURCE_TYPES: { label: string; value: string }[] = [
  { label: 'Customer Invoice', value: 'CustomerInvoice' },
  { label: 'Quotation', value: 'Quotation' },
  { label: 'Purchase Order', value: 'PurchaseOrder' },
  { label: 'Proforma Invoice', value: 'ProformaInvoice' },
  { label: 'Receipt', value: 'Receipt' }
];

export interface SentEmailDto {
  id: number;
  sentAt: string;
  sentByUser: string;
  sourceType: string | null;
  sourceId: number | null;
  sourceCode: string | null;
  toAddresses: string;
  ccAddresses: string | null;
  subject: string;
  status: string;
  errorMessage: string | null;
}

export interface EmailPreviewDto {
  sourceType: string;
  sourceId: number;
  sourceCode: string;
  defaultSubject: string;
  htmlBody: string;
  defaultToAddress: string | null;
}

export interface SendDocumentEmailRequest {
  sourceType: string;
  sourceId: number;
  toAddresses: string;          // comma or semicolon separated
  ccAddresses: string | null;
  subject: string;
  htmlBody: string;
}
