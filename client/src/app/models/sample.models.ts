// ─── Sample Development ───────────────────────────────────────────────────

export const SAMPLE_STATUSES: { label: string; value: string }[] = [
  { label: 'Requested', value: 'Requested' },
  { label: 'In Development', value: 'InDevelopment' },
  { label: 'Submitted', value: 'Submitted' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Rejected', value: 'Rejected' }
];

export interface SampleDto {
  id: number;
  code: string;
  customerId: number;
  customerName: string;
  productId: number | null;
  productName: string | null;
  styleId: number | null;
  styleName: string | null;
  title: string;
  description: string | null;
  buyerReference: string | null;
  quantity: number;
  requestedDate: string;
  targetDate: string | null;
  status: string;
  submittedDate: string | null;
  decidedAt: string | null;
  decidedBy: string | null;
  feedback: string | null;
  leadTimeDays: number | null;
  notes: string | null;
}

export interface SampleListItemDto {
  id: number;
  code: string;
  customerName: string;
  title: string;
  productName: string | null;
  quantity: number;
  requestedDate: string;
  targetDate: string | null;
  status: string;
  leadTimeDays: number | null;
}

export interface SaveSampleRequest {
  id?: number;
  customerId: number | null;
  productId: number | null;
  styleId: number | null;
  title: string;
  description: string | null;
  buyerReference: string | null;
  quantity: number;
  requestedDate: string;
  targetDate: string | null;
  notes: string | null;
}
