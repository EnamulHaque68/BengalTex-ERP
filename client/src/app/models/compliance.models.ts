// ─── Compliance ─────────────────────────────────────────────────────────────

export type CertificateType =
  | 'BSCI' | 'Sedex' | 'WRAP' | 'SA8000' | 'ISO9001' | 'ISO14001'
  | 'TradeLicense' | 'FireLicense' | 'FactoryLicense' | 'EnvironmentClearance'
  | 'BondLicense' | 'BoilerCertificate' | 'OEKO_TEX' | 'GOTS' | 'Other';

export const CERTIFICATE_TYPES: { label: string; value: CertificateType }[] = [
  { label: 'BSCI', value: 'BSCI' },
  { label: 'Sedex', value: 'Sedex' },
  { label: 'WRAP', value: 'WRAP' },
  { label: 'SA8000', value: 'SA8000' },
  { label: 'ISO 9001', value: 'ISO9001' },
  { label: 'ISO 14001', value: 'ISO14001' },
  { label: 'Trade License', value: 'TradeLicense' },
  { label: 'Fire License', value: 'FireLicense' },
  { label: 'Factory License', value: 'FactoryLicense' },
  { label: 'Environment Clearance', value: 'EnvironmentClearance' },
  { label: 'Bond License', value: 'BondLicense' },
  { label: 'Boiler Certificate', value: 'BoilerCertificate' },
  { label: 'OEKO-TEX', value: 'OEKO_TEX' },
  { label: 'GOTS', value: 'GOTS' },
  { label: 'Other', value: 'Other' }
];

export type ExpiryStatus = 'Active' | 'ExpiringSoon' | 'Expired';

export const EXPIRY_STATUSES: { label: string; value: ExpiryStatus }[] = [
  { label: 'Active', value: 'Active' },
  { label: 'Expiring Soon (≤60d)', value: 'ExpiringSoon' },
  { label: 'Expired', value: 'Expired' }
];

export interface ComplianceCertificateDto {
  id: number;
  name: string;
  certificateType: CertificateType;
  issuingAuthority: string | null;
  certificateNumber: string | null;
  issuedDate: string;
  expiryDate: string;
  daysUntilExpiry: number;
  expiryStatus: ExpiryStatus;
  notes: string | null;
  isActive: boolean;
}

export interface SaveCertificateRequest {
  name: string;
  certificateType: CertificateType;
  issuingAuthority: string | null;
  certificateNumber: string | null;
  issuedDate: string;
  expiryDate: string;
  notes: string | null;
  isActive?: boolean;
}

// ── Audit ──
export type AuditType = 'BSCI' | 'Sedex' | 'WRAP' | 'SA8000' | 'BuyerAudit' | 'Internal' | 'Government' | 'Other';

export const AUDIT_TYPES: { label: string; value: AuditType }[] = [
  { label: 'BSCI', value: 'BSCI' },
  { label: 'Sedex', value: 'Sedex' },
  { label: 'WRAP', value: 'WRAP' },
  { label: 'SA8000', value: 'SA8000' },
  { label: 'Buyer Audit', value: 'BuyerAudit' },
  { label: 'Internal', value: 'Internal' },
  { label: 'Government', value: 'Government' },
  { label: 'Other', value: 'Other' }
];

export type AuditStatus = 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled';
export const AUDIT_STATUSES: { label: string; value: AuditStatus }[] = [
  { label: 'Scheduled', value: 'Scheduled' },
  { label: 'In Progress', value: 'InProgress' },
  { label: 'Completed', value: 'Completed' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export type AuditResult = 'Pass' | 'Conditional' | 'Fail' | 'PendingCorrection';
export const AUDIT_RESULTS: { label: string; value: AuditResult }[] = [
  { label: 'Pass', value: 'Pass' },
  { label: 'Conditional', value: 'Conditional' },
  { label: 'Fail', value: 'Fail' },
  { label: 'Pending Correction', value: 'PendingCorrection' }
];

export type FindingSeverity = 'Critical' | 'Major' | 'Minor' | 'Observation';
export const SEVERITIES: { label: string; value: FindingSeverity }[] = [
  { label: 'Critical', value: 'Critical' },
  { label: 'Major', value: 'Major' },
  { label: 'Minor', value: 'Minor' },
  { label: 'Observation', value: 'Observation' }
];

export type FindingStatus = 'Open' | 'InProgress' | 'Closed' | 'Waived';
export const FINDING_STATUSES: { label: string; value: FindingStatus }[] = [
  { label: 'Open', value: 'Open' },
  { label: 'In Progress', value: 'InProgress' },
  { label: 'Closed', value: 'Closed' },
  { label: 'Waived', value: 'Waived' }
];

export interface AuditFindingDto {
  id: number;
  complianceAuditId: number;
  findingDescription: string;
  severity: FindingSeverity;
  correctiveAction: string | null;
  assignedToEmployeeId: number | null;
  assignedToEmployeeName: string | null;
  dueDate: string | null;
  closureDate: string | null;
  status: FindingStatus;
  isOverdue: boolean;
  notes: string | null;
}

export interface ComplianceAuditDto {
  id: number;
  code: string;
  auditType: AuditType;
  auditor: string;
  scheduledDate: string;
  actualDate: string | null;
  status: AuditStatus;
  result: AuditResult | null;
  score: number | null;
  notes: string | null;
  findings: AuditFindingDto[];
}

export interface ComplianceAuditListItemDto {
  id: number;
  code: string;
  auditType: AuditType;
  auditor: string;
  scheduledDate: string;
  actualDate: string | null;
  status: AuditStatus;
  result: AuditResult | null;
  score: number | null;
  openFindings: number;
}

export interface CreateAuditRequest {
  auditType: AuditType;
  auditor: string;
  scheduledDate: string;
  notes: string | null;
}

export interface UpdateAuditRequest {
  auditor: string;
  scheduledDate: string;
  actualDate: string | null;
  status: AuditStatus;
  result: AuditResult | null;
  score: number | null;
  notes: string | null;
}

export interface AddFindingRequest {
  findingDescription: string;
  severity: FindingSeverity;
  correctiveAction: string | null;
  assignedToEmployeeId: number | null;
  dueDate: string | null;
  notes: string | null;
}

export interface UpdateFindingRequest {
  findingDescription: string;
  severity: FindingSeverity;
  correctiveAction: string | null;
  assignedToEmployeeId: number | null;
  dueDate: string | null;
  status: FindingStatus;
  closureDate: string | null;
  notes: string | null;
}

export interface ComplianceDashboardDto {
  certificatesActive: number;
  certificatesExpiringSoon: number;
  certificatesExpired: number;
  openFindings: number;
  overdueFindings: number;
  upcomingAudits: number;
  expiringCertificates: ComplianceCertificateDto[];
  overdueFindingsList: AuditFindingDto[];
}
