// ─── Audit Log ────────────────────────────────────────────────────────────

export const AUDIT_ACTIONS: { label: string; value: string }[] = [
  { label: 'Insert', value: 'Insert' },
  { label: 'Update', value: 'Update' },
  { label: 'Delete', value: 'Delete' }
];

export interface AuditLogEntryDto {
  id: number;
  entityType: string;
  entityKey: string;
  action: string;                     // Insert | Update | Delete
  userName: string | null;
  ipAddress: string | null;
  affectedColumns: string | null;
  oldValuesJson: string | null;
  newValuesJson: string | null;
  timestamp: string;
}
