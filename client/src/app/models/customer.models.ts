export type CustomerCategoryName = 'A' | 'B' | 'C';

export const CUSTOMER_CATEGORIES: CustomerCategoryName[] = ['A', 'B', 'C'];

export interface CustomerDto {
  id: number;
  code: string;
  name: string;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  website: string | null;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  district: string | null;
  postalCode: string | null;
  country: string;
  binNumber: string | null;
  vatNumber: string | null;
  tinNumber: string | null;
  category: CustomerCategoryName;
  creditLimit: number;
  creditPeriodDays: number;
  notes: string | null;
  isActive: boolean;
}

export interface CustomerListItemDto {
  id: number;
  code: string;
  name: string;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  city: string;
  category: CustomerCategoryName;
  creditLimit: number;
  creditPeriodDays: number;
  isActive: boolean;
}

export interface CreateCustomerRequest {
  code: string | null;          // null = auto-generate
  name: string;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  website: string | null;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  district: string | null;
  postalCode: string | null;
  country: string;
  binNumber: string | null;
  vatNumber: string | null;
  tinNumber: string | null;
  category: CustomerCategoryName;
  creditLimit: number;
  creditPeriodDays: number;
  notes: string | null;
}

export interface UpdateCustomerRequest {
  name: string;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  website: string | null;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  district: string | null;
  postalCode: string | null;
  country: string;
  binNumber: string | null;
  vatNumber: string | null;
  tinNumber: string | null;
  category: CustomerCategoryName;
  creditLimit: number;
  creditPeriodDays: number;
  notes: string | null;
  isActive: boolean;
}
