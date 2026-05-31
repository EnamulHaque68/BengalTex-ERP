// ─── Machine + Job Card models ────────────────────────────────────────────

export interface MachineDto {
  id: number;
  code: string;
  name: string;
  machineType: string | null;
  location: string | null;
  capacityPerHour: number | null;
  notes: string | null;
  isActive: boolean;
}

export interface SaveMachineRequest {
  code?: string | null;
  name: string;
  machineType: string | null;
  location: string | null;
  capacityPerHour: number | null;
  notes: string | null;
  isActive?: boolean;        // only used on Update
}

export type JobCardStatus = 'Open' | 'InProgress' | 'OnHold' | 'Completed' | 'Cancelled';

export const JOB_CARD_STATUSES: { label: string; value: JobCardStatus }[] = [
  { label: 'Open', value: 'Open' },
  { label: 'In Progress', value: 'InProgress' },
  { label: 'On Hold', value: 'OnHold' },
  { label: 'Completed', value: 'Completed' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface JobCardScanDto {
  id: number;
  scanType: 'Start' | 'Pause' | 'Resume' | 'Complete' | 'QcCheck' | 'Cancel';
  scannedAt: string;
  scannedBy: string | null;
  quantity: number | null;
  rejectedQuantity: number | null;
  notes: string | null;
}

export interface JobCardListItemDto {
  id: number;
  code: string;
  productionOrderId: number;
  productionOrderCode: string;
  productName: string;
  batchNumber: string | null;
  quantity: number;
  completedQuantity: number;
  rejectedQuantity: number;
  machineName: string | null;
  operatorName: string | null;
  status: JobCardStatus;
  startedAt: string | null;
  completedAt: string | null;
  activeMinutes: number | null;
}

export interface JobCardDto {
  id: number;
  code: string;
  productionOrderId: number;
  productionOrderCode: string;
  productName: string;
  productionStageId: number | null;
  stageName: string | null;
  batchNumber: string | null;
  quantity: number;
  completedQuantity: number;
  rejectedQuantity: number;
  machineId: number | null;
  machineCode: string | null;
  machineName: string | null;
  operatorEmployeeId: number | null;
  operatorCode: string | null;
  operatorName: string | null;
  status: JobCardStatus;
  startedAt: string | null;
  lastResumedAt: string | null;
  completedAt: string | null;
  completedBy: string | null;
  activeMinutes: number | null;
  notes: string | null;
  scans: JobCardScanDto[];
}

export interface CreateJobCardRequest {
  productionOrderId: number;
  productionStageId: number | null;
  batchNumber: string | null;
  quantity: number;
  machineId: number | null;
  operatorEmployeeId: number | null;
  notes: string | null;
}

export interface UpdateJobCardRequest {
  batchNumber: string | null;
  quantity: number;
  machineId: number | null;
  operatorEmployeeId: number | null;
  notes: string | null;
}

export interface ScanJobCardRequest {
  jobCardId?: number | null;
  code?: string | null;
  scanType: 'Start' | 'Pause' | 'Resume' | 'Complete' | 'QcCheck' | 'Cancel';
  quantity?: number | null;
  rejectedQuantity?: number | null;
  notes?: string | null;
}

export interface JobCardBoardCountsDto {
  open: number;
  inProgress: number;
  onHold: number;
  completed: number;
  cancelled: number;
}
