// ─── Employee (HR) ────────────────────────────────────────────────────────

export const GENDERS: { label: string; value: string }[] = [
  { label: 'Male', value: 'Male' },
  { label: 'Female', value: 'Female' },
  { label: 'Other', value: 'Other' }
];

export const EMPLOYMENT_TYPES: { label: string; value: string }[] = [
  { label: 'Permanent', value: 'Permanent' },
  { label: 'Contract', value: 'Contract' },
  { label: 'Daily Wage', value: 'DailyWage' }
];

export const EMPLOYEE_STATUSES: { label: string; value: string }[] = [
  { label: 'Active', value: 'Active' },
  { label: 'Inactive', value: 'Inactive' },
  { label: 'Terminated', value: 'Terminated' }
];

export interface EmployeeDto {
  id: number;
  code: string;
  fullName: string;
  designation: string | null;
  department: string | null;
  phone: string | null;
  email: string | null;
  nationalId: string | null;
  address: string | null;
  joiningDate: string;               // "YYYY-MM-DD"
  dateOfBirth: string | null;
  gender: string;
  employmentType: string;
  basicSalary: number;
  houseRentAllowance: number;
  medicalAllowance: number;
  transportAllowance: number;
  foodAllowance: number;
  isPfMember: boolean;
  pfRate: number;
  isTaxable: boolean;
  departmentId: number | null;
  designationId: number | null;
  shiftId: number | null;
  bankAccountId: number | null;
  status: string;
  notes: string | null;
  isActive: boolean;
}

export interface EmployeeListItemDto {
  id: number;
  code: string;
  fullName: string;
  designation: string | null;
  department: string | null;
  phone: string | null;
  employmentType: string;
  basicSalary: number;
  status: string;
  isActive: boolean;
}

export interface CreateEmployeeRequest {
  code: string | null;
  fullName: string;
  designation: string | null;
  department: string | null;
  phone: string | null;
  email: string | null;
  nationalId: string | null;
  address: string | null;
  joiningDate: string;
  dateOfBirth: string | null;
  gender: string;
  employmentType: string;
  basicSalary: number;
  houseRentAllowance: number;
  medicalAllowance: number;
  transportAllowance: number;
  foodAllowance: number;
  isPfMember: boolean;
  pfRate: number;
  isTaxable: boolean;
  departmentId: number | null;
  designationId: number | null;
  shiftId: number | null;
  bankAccountId: number | null;
  notes: string | null;
}

export interface UpdateEmployeeRequest {
  fullName: string;
  designation: string | null;
  department: string | null;
  phone: string | null;
  email: string | null;
  nationalId: string | null;
  address: string | null;
  joiningDate: string;
  dateOfBirth: string | null;
  gender: string;
  employmentType: string;
  basicSalary: number;
  houseRentAllowance: number;
  medicalAllowance: number;
  transportAllowance: number;
  foodAllowance: number;
  isPfMember: boolean;
  pfRate: number;
  isTaxable: boolean;
  departmentId: number | null;
  designationId: number | null;
  shiftId: number | null;
  bankAccountId: number | null;
  status: string;
  notes: string | null;
  isActive: boolean;
}
