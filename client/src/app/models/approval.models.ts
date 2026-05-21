// ─── Approvals / Workflow ───────────────────────────────────────────────────

export const APPROVAL_STATUSES: { label: string; value: string }[] = [
  { label: 'Pending', value: 'Pending' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Rejected', value: 'Rejected' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface ApprovalStepDto {
  id: number;
  level: number;
  approverRole: string;
  approverUserId: string | null;
  status: string;                       // Pending | Approved | Rejected | Skipped
  actedAt: string | null;
  comment: string | null;
}

export interface ApprovalRequestDto {
  id: number;
  documentType: string;
  documentId: number;
  documentReference: string;
  status: string;                       // Pending | Approved | Rejected | Cancelled
  currentLevel: number;
  totalLevels: number;
  currentApproverRole: string | null;
  requestedBy: string | null;
  requestedAt: string;
  completedAt: string | null;
  steps: ApprovalStepDto[];
}
