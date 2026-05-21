// ─── Attendance ───────────────────────────────────────────────────────────

export const ATTENDANCE_STATUSES: { label: string; value: string }[] = [
  { label: 'Present', value: 'Present' },
  { label: 'Absent', value: 'Absent' },
  { label: 'Late', value: 'Late' },
  { label: 'Half Day', value: 'HalfDay' },
  { label: 'Leave', value: 'Leave' },
  { label: 'Holiday', value: 'Holiday' }
];

export interface AttendanceRecordDto {
  id: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  attendanceDate: string;            // "YYYY-MM-DD"
  status: string;
  checkInTime: string | null;        // "HH:mm"
  checkOutTime: string | null;
  overtimeHours: number;
  notes: string | null;
}

export interface CreateAttendanceRequest {
  employeeId: number;
  attendanceDate: string;
  status: string;
  checkInTime: string | null;
  checkOutTime: string | null;
  overtimeHours: number;
  notes: string | null;
}

export interface UpdateAttendanceRequest {
  status: string;
  checkInTime: string | null;
  checkOutTime: string | null;
  overtimeHours: number;
  notes: string | null;
}
