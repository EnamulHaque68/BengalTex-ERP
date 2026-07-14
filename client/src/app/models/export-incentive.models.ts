// ─── Phase A6b — export cash-incentive claims ──────────────────────────────

export const INCENTIVE_STATUSES: { label: string; value: string }[] = [
  { label: 'Accrued', value: 'Accrued' },
  { label: 'Received', value: 'Received' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface ExportIncentiveClaimDto {
  id: number;
  code: string;
  customerInvoiceId: number | null;
  customerInvoiceCode: string | null;
  exportReference: string | null;
  incentiveRate: number;
  amount: number;
  claimDate: string;
  status: string;
  receivedDate: string | null;
  receivedMethod: string | null;
  bankReference: string | null;
  notes: string | null;
}

export interface ExportIncentiveListDto {
  items: ExportIncentiveClaimDto[];
  outstandingReceivable: number;
}

export interface CreateExportIncentiveRequest {
  customerInvoiceId: number | null;
  exportReference: string | null;
  incentiveRate: number;
  amount: number;
  claimDate: string;
  notes: string | null;
}

export interface MarkIncentiveReceivedRequest {
  receivedDate: string;
  paymentMethod: string;
  bankReference: string | null;
}
