// ─── Leave Management ─────────────────────────────────────────────────────

export type LeaveApplicationStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

export const LEAVE_STATUSES: { label: string; value: LeaveApplicationStatus }[] = [
  { label: 'Pending', value: 'Pending' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Rejected', value: 'Rejected' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface LeaveTypeDto {
  id: number;
  code: string;
  name: string;
  isPaid: boolean;
  annualEntitlement: number;
  maxConsecutiveDays: number | null;
  description: string | null;
  isActive: boolean;
}

export interface SaveLeaveTypeRequest {
  code?: string;     // only on create
  name: string;
  isPaid: boolean;
  annualEntitlement: number;
  maxConsecutiveDays: number | null;
  description: string | null;
  isActive?: boolean;
}

export interface HolidayDto {
  id: number;
  date: string;        // "YYYY-MM-DD"
  name: string;
  description: string | null;
  isActive: boolean;
}

export interface SaveHolidayRequest {
  date: string;
  name: string;
  description: string | null;
  isActive?: boolean;
}

export interface LeaveBalanceDto {
  id: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  leaveTypeId: number;
  leaveTypeCode: string;
  leaveTypeName: string;
  year: number;
  entitled: number;
  taken: number;
  remaining: number;
}

export interface LeaveApplicationListItemDto {
  id: number;
  code: string;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  leaveTypeCode: string;
  leaveTypeName: string;
  fromDate: string;
  toDate: string;
  totalDays: number;
  status: LeaveApplicationStatus;
  reason: string | null;
}

export interface LeaveApplicationDto {
  id: number;
  code: string;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  leaveTypeId: number;
  leaveTypeCode: string;
  leaveTypeName: string;
  fromDate: string;
  toDate: string;
  totalDays: number;
  reason: string | null;
  status: LeaveApplicationStatus;
  decidedAt: string | null;
  decidedBy: string | null;
  rejectionReason: string | null;
  writeAttendance: boolean;
  notes: string | null;
}

export interface CreateLeaveApplicationRequest {
  employeeId: number;
  leaveTypeId: number;
  fromDate: string;
  toDate: string;
  reason: string | null;
  writeAttendance: boolean;
  notes: string | null;
}
