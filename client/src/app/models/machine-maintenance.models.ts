// ─── Machine Maintenance ──────────────────────────────────────────────────

export const MAINTENANCE_TYPES: { label: string; value: string }[] = [
  { label: 'Preventive', value: 'Preventive' },
  { label: 'Corrective (Breakdown)', value: 'Corrective' },
  { label: 'Inspection', value: 'Inspection' },
  { label: 'Calibration', value: 'Calibration' },
  { label: 'Overhaul', value: 'Overhaul' },
  { label: 'Cleaning', value: 'Cleaning' }
];

export const MAINTENANCE_STATUSES: { label: string; value: string }[] = [
  { label: 'Scheduled', value: 'Scheduled' },
  { label: 'In Progress', value: 'InProgress' },
  { label: 'Completed', value: 'Completed' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface MachineMaintenanceDto {
  id: number;
  code: string;
  machineId: number;
  machineCode: string;
  machineName: string;
  machineType: string | null;
  machineLocation: string | null;
  type: string;
  description: string;
  scheduledDate: string;
  completedDate: string | null;
  downtimeHours: number | null;
  performedBy: string | null;
  performedByEmployeeId: number | null;
  performedByEmployeeName: string | null;
  serviceCost: number;
  partsCost: number;
  totalCost: number;
  partsReplaced: string | null;
  completionNotes: string | null;
  status: string;
  isOverdue: boolean;
  isRecurring: boolean;
  intervalDays: number | null;
  recurringSeriesAnchorId: number | null;
  notes: string | null;
}

export interface ScheduleMaintenanceRequest {
  machineId: number;
  type: string;
  description: string;
  scheduledDate: string;
  isRecurring: boolean;
  intervalDays: number | null;
  notes: string | null;
}

export interface UpdateMaintenanceRequest {
  type: string;
  description: string;
  scheduledDate: string;
  isRecurring: boolean;
  intervalDays: number | null;
  notes: string | null;
}

export interface CompleteMaintenanceRequest {
  completedDate: string;
  downtimeHours: number | null;
  performedBy: string | null;
  performedByEmployeeId: number | null;
  serviceCost: number;
  partsCost: number;
  partsReplaced: string | null;
  completionNotes: string | null;
}
