// ─── Gate Pass ───────────────────────────────────────────────────────────

export const GATE_PASS_TYPES: { label: string; value: string }[] = [
  { label: 'Non-Returnable Out', value: 'NonReturnableOut' },
  { label: 'Returnable Out', value: 'ReturnableOut' },
  { label: 'Inward Receipt', value: 'InwardReceipt' },
  { label: 'Visitor', value: 'Visitor' },
  { label: 'Vehicle', value: 'Vehicle' }
];

export const GATE_PASS_DIRECTIONS: { label: string; value: string }[] = [
  { label: 'Out', value: 'Out' },
  { label: 'In', value: 'In' }
];

export const GATE_PASS_STATUSES: { label: string; value: string }[] = [
  { label: 'Open', value: 'Open' },
  { label: 'Returned', value: 'Returned' },
  { label: 'Closed', value: 'Closed' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface GatePassDto {
  id: number;
  code: string;
  passDate: string;
  passTime: string | null;
  type: string;
  direction: string;
  vehicleNumber: string | null;
  driverName: string | null;
  driverPhone: string | null;
  driverNidNumber: string | null;
  transporterName: string | null;
  visitorName: string | null;
  visitorPhone: string | null;
  visitorOrganization: string | null;
  visitorPurpose: string | null;
  itemDescription: string | null;
  quantity: string | null;
  fromLocation: string | null;
  toLocation: string | null;
  sourceType: string | null;
  sourceId: number | null;
  sourceCode: string | null;
  issuedByUser: string | null;
  approvedByUser: string | null;
  expectedReturnDate: string | null;
  returnedAt: string | null;
  returnedByUser: string | null;
  returnNotes: string | null;
  closedAt: string | null;
  status: string;
  isOverdue: boolean;
  notes: string | null;
}

export interface SaveGatePassRequest {
  passDate: string;
  passTime: string | null;
  type: string;
  direction: string;
  vehicleNumber: string | null;
  driverName: string | null;
  driverPhone: string | null;
  driverNidNumber: string | null;
  transporterName: string | null;
  visitorName: string | null;
  visitorPhone: string | null;
  visitorOrganization: string | null;
  visitorPurpose: string | null;
  itemDescription: string | null;
  quantity: string | null;
  fromLocation: string | null;
  toLocation: string | null;
  sourceType: string | null;
  sourceId: number | null;
  sourceCode: string | null;
  approvedByUser: string | null;
  expectedReturnDate: string | null;
  notes: string | null;
}
