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
  // Geo-fence verification
  checkInLatitude: number | null;
  checkInLongitude: number | null;
  checkInDistanceMeters: number | null;
  checkInWithinFence: boolean | null;
  // Location & network intelligence (P2)
  checkInAddress?: string | null;
  checkInIpAddress?: string | null;
  checkInDeviceType?: string | null;
  checkInBrowser?: string | null;
  checkInOs?: string | null;
  checkInIsProxyVpn?: boolean | null;
  checkInIsp?: string | null;
  checkInNetworkNote?: string | null;
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

export interface SelfCheckInRequest {
  latitude: number | null;
  longitude: number | null;
  notes: string | null;
  selfieBase64?: string | null;
}

export interface SelfCheckOutRequest {
  latitude: number | null;
  longitude: number | null;
  selfieBase64?: string | null;
}

// ─── My Attendance (self-service dashboard) ─────────────────────────────────

export interface MyBreakDto {
  breakOutTime: string | null;
  breakInTime: string | null;
  minutes: number | null;
}

export interface MyAttendanceTodayDto {
  hasRecord: boolean;
  checkInTime: string | null;
  checkOutTime: string | null;
  status: string;
  isLate: boolean;
  isEarlyLeave: boolean;
  workedMinutes: number | null;
  workingHoursLabel: string;          // "08h 12m"
  onBreak: boolean;
  locationStatus: string | null;      // "Within Office Area" | "Outside Office Area"
  distanceMeters: number | null;
  withinFence: boolean | null;
  matchedLocationName: string | null;
  latitude: number | null;
  longitude: number | null;
  hasSelfie: boolean;
  approvalStatus: string;             // NotRequired | Pending | Approved | Rejected
  // Location & network intelligence (P2)
  address: string | null;
  deviceType: string | null;
  browser: string | null;
  os: string | null;
  isProxyVpn: boolean | null;
  networkNote: string | null;
  ipAddress: string | null;
  network: string | null;
  breaks: MyBreakDto[];
}

export interface MyMonthSummaryDto {
  year: number;
  month: number;
  presentDays: number;
  absentDays: number;
  leaveDays: number;
  lateDays: number;
  overtimeHours: number;
}

export interface MyCalendarDayDto {
  date: string;                       // "YYYY-MM-DD"
  status: string;
}

export interface MyAlertDto {
  severity: string;                   // critical | warning | info
  title: string;
  detail: string | null;
  time: string | null;
}

export interface MyHistoryDto {
  time: string;                       // "HH:mm"
  event: string;                      // Check In | Break Out | Break In | Check Out
  location: string | null;
  remark: string | null;
}

export interface MyAttendanceDto {
  employeeId: number;
  employeeName: string;
  employeeCode: string;
  designation: string | null;
  department: string | null;
  today: string;                      // "YYYY-MM-DD"
  todayStatus: MyAttendanceTodayDto;
  month: MyMonthSummaryDto;
  calendar: MyCalendarDayDto[];
  alerts: MyAlertDto[];
  history: MyHistoryDto[];
  requireSelfie: boolean;
  officeStartTime: string;            // "HH:mm"
  officeEndTime: string;              // "HH:mm"
}

// ─── Supervisor: Team Attendance + review (P3) ──────────────────────────────

export interface AttendanceFlag {
  code: string;
  label: string;
  severity: string;                   // critical | warning | info
}

export interface TeamAttendanceRowDto {
  id: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  designation: string | null;
  department: string | null;
  attendanceDate: string;
  status: string;
  checkInTime: string | null;
  checkOutTime: string | null;
  workingHoursLabel: string | null;
  isLate: boolean;
  isEarlyLeave: boolean;
  hasCheckInSelfie: boolean;
  hasCheckOutSelfie: boolean;
  withinFence: boolean | null;
  distanceMeters: number | null;
  matchedLocationName: string | null;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  checkOutWithinFence: boolean | null;
  checkOutDistanceMeters: number | null;
  checkOutAddress: string | null;
  checkOutLatitude: number | null;
  checkOutLongitude: number | null;
  deviceType: string | null;
  browser: string | null;
  os: string | null;
  isProxyVpn: boolean | null;
  networkNote: string | null;
  ipAddress: string | null;
  approvalStatus: string;             // NotRequired | Pending | Approved | Rejected
  approvedByName: string | null;
  approvedAt: string | null;
  rejectionReason: string | null;
  flags: AttendanceFlag[];
}

export interface TeamAttendanceSummaryDto {
  teamSize: number;
  presentToday: number;
  pendingApprovals: number;
  flaggedToday: number;
}

export interface TeamAttendanceDto {
  supervisorEmployeeId: number;
  seesAll: boolean;
  summary: TeamAttendanceSummaryDto;
  rows: TeamAttendanceRowDto[];
}

export interface AttendanceRequestDto {
  id: number;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  requestDate: string;
  requestType: string;
  requestedCheckInTime: string | null;
  requestedCheckOutTime: string | null;
  requestedStatus: string | null;
  reason: string;
  status: string;                     // Pending | Approved | Rejected | Cancelled
  reviewedByName: string | null;
  reviewedAt: string | null;
  reviewNote: string | null;
  createdAt: string;
}

export interface SubmitAttendanceRequest {
  requestDate: string;
  requestType: string;
  requestedCheckInTime: string | null;
  requestedCheckOutTime: string | null;
  requestedStatus: string | null;
  reason: string;
}

export const ATTENDANCE_REQUEST_TYPES: { label: string; value: string }[] = [
  { label: 'Missing Check-In', value: 'MissingCheckIn' },
  { label: 'Missing Check-Out', value: 'MissingCheckOut' },
  { label: 'Time Correction', value: 'TimeCorrection' },
  { label: 'Regularization', value: 'Regularization' },
  { label: 'Off-day Work', value: 'OffdayWork' },
  { label: 'Other', value: 'Other' }
];

// ─── Settings + Office Locations + Reports (P4) ─────────────────────────────

export interface AttendanceSettingsDto {
  id: number;
  officeStartTime: string;            // "HH:mm"
  officeEndTime: string;
  gracePeriodMinutes: number;
  outsideFenceMode: string;           // Flag | Block
  defaultRadiusMeters: number;
  requireSelfie: boolean;
  requireSupervisorApproval: boolean;
  allowRemote: boolean;
  allowFieldVisit: boolean;
}

export const OFFICE_LOCATION_TYPES: { label: string; value: string }[] = [
  { label: 'Head Office', value: 'HeadOffice' },
  { label: 'Factory', value: 'Factory' },
  { label: 'Warehouse', value: 'Warehouse' },
  { label: 'Branch Office', value: 'BranchOffice' }
];

export interface OfficeLocationDto {
  id: number;
  name: string;
  type: string;
  latitude: number;
  longitude: number;
  radiusMeters: number;
  address: string | null;
  isActive: boolean;
  assignedEmployeeCount: number;
}

export interface OfficeLocationEmployeeDto {
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  designation: string | null;
  department: string | null;
  assigned: boolean;
}

export interface UpsertOfficeLocation {
  name: string;
  type: string;
  latitude: number;
  longitude: number;
  radiusMeters: number;
  address: string | null;
  isActive: boolean;
}

// Reports
export interface DailyRegisterRowDto {
  employeeId: number; employeeCode: string; employeeName: string; department: string | null;
  status: string; checkInTime: string | null; checkOutTime: string | null;
  workingHoursLabel: string | null; isLate: boolean; withinFence: boolean | null; hasRecord: boolean;
}
export interface DailyRegisterDto {
  date: string; isHoliday: boolean; holidayName: string | null;
  total: number; present: number; absent: number; late: number; onLeave: number;
  rows: DailyRegisterRowDto[];
}

export interface MonthlySummaryRowDto {
  employeeId: number; employeeCode: string; employeeName: string; department: string | null;
  presentDays: number; absentDays: number; lateDays: number; leaveDays: number;
  holidayWorkDays: number; offdayWorkDays: number; overtimeHours: number; totalWorkedLabel: string;
}
export interface MonthlySummaryDto {
  year: number; month: number; workingEmployees: number; rows: MonthlySummaryRowDto[];
}

export interface AttendanceExceptionRowDto {
  id: number; employeeId: number; employeeCode: string; employeeName: string; department: string | null;
  attendanceDate: string; status: string; checkInTime: string | null; checkOutTime: string | null;
  exceptionType: string; detail: string;
}
export interface AttendanceExceptionsDto {
  fromDate: string; toDate: string; type: string; count: number; rows: AttendanceExceptionRowDto[];
}

export const ATTENDANCE_EXCEPTION_TYPES: { label: string; value: string }[] = [
  { label: 'Late Arrivals', value: 'Late' },
  { label: 'Absentees', value: 'Absent' },
  { label: 'Outside Geo-fence', value: 'OutsideFence' },
  { label: 'VPN / Proxy', value: 'ProxyVpn' },
  { label: 'Missing Check-out', value: 'MissingCheckout' },
  { label: 'Overtime', value: 'Overtime' },
  { label: 'Pending Approval', value: 'PendingApproval' }
];
