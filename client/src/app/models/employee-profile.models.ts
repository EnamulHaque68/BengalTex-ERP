// ─── Employee Profile ───────────────────────────────────────────────────────

export const MARITAL_STATUSES: { label: string; value: string }[] = [
  { label: 'Single', value: 'Single' },
  { label: 'Married', value: 'Married' },
  { label: 'Divorced', value: 'Divorced' },
  { label: 'Widowed', value: 'Widowed' }
];

export const BLOOD_GROUPS: { label: string; value: string }[] =
  ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'].map(g => ({ label: g, value: g }));

export interface ProfileLeaveBalanceDto {
  leaveTypeName: string;
  entitled: number;
  taken: number;
  remaining: number;
}

export interface ProfilePayslipDto {
  payslipId: number;
  year: number;
  month: number;
  monthLabel: string;
  netPay: number;
  status: string;
}

export interface ProfileAttendanceSummaryDto {
  year: number;
  month: number;
  monthLabel: string;
  presentDays: number;
  lateDays: number;
  absentDays: number;
  leaveDays: number;
  totalWorkingDays: number;
}

export interface ProfileSkillDto {
  id: number;
  name: string;
  proficiencyPercent: number;
}

export interface ProfileEducationDto {
  id: number;
  degree: string;
  institute: string | null;
  passingYear: number | null;
  result: string | null;
}

export interface ProfileEmergencyContactDto {
  id: number;
  name: string;
  relationship: string | null;
  phone: string;
  address: string | null;
}

export interface SaveEducationRequest {
  id: number;
  degree: string;
  institute: string | null;
  passingYear: number | null;
  result: string | null;
}

export interface SaveContactRequest {
  id: number;
  name: string;
  relationship: string | null;
  phone: string;
  address: string | null;
}

export interface ProfileActivityDto {
  id: number;
  entityType: string;
  entityKey: string;
  action: string;            // Insert | Update | Delete
  userName: string | null;
  ipAddress: string | null;
  affectedColumns: string | null;
  oldValuesJson: string | null;
  newValuesJson: string | null;
  timestamp: string;
}

export interface EmployeeProfileDto {
  id: number;
  code: string;
  fullName: string;
  designation: string | null;
  department: string | null;
  photoUrl: string | null;
  isActive: boolean;
  status: string;
  joiningDate: string;
  employmentType: string;
  workLocation: string | null;
  email: string | null;
  phone: string | null;
  dateOfBirth: string | null;
  nationality: string | null;
  bloodGroup: string | null;
  gender: string;
  maritalStatus: string;
  religion: string | null;
  nationalId: string | null;
  address: string | null;
  aboutMe: string | null;
  reportingToEmployeeId: number | null;
  reportingToName: string | null;
  probationEndDate: string | null;
  confirmationDate: string | null;
  basicSalary: number;
  houseRentAllowance: number;
  medicalAllowance: number;
  transportAllowance: number;
  foodAllowance: number;
  grossSalary: number;
  bankName: string | null;
  accountNumberMasked: string | null;
  leaveBalances: ProfileLeaveBalanceDto[];
  latestPayslips: ProfilePayslipDto[];
  skills: ProfileSkillDto[];
  education: ProfileEducationDto[];
  emergencyContacts: ProfileEmergencyContactDto[];
  attendance: ProfileAttendanceSummaryDto;
  userId: string | null;
  canEdit: boolean;
}

export interface EmployeeSkillRequest {
  name: string;
  proficiencyPercent: number;
}

export interface UpdateEmployeeProfileRequest {
  employeeId: number;
  photoUrl: string | null;
  bloodGroup: string | null;
  maritalStatus: string;
  religion: string | null;
  nationality: string | null;
  workLocation: string | null;
  aboutMe: string | null;
  probationEndDate: string | null;
  confirmationDate: string | null;
  reportingToEmployeeId: number | null;
  userId: string | null;
}
