export interface SupplierDto {
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
  paymentTermsDays: number;
  bankName: string | null;
  bankAccountNumber: string | null;
  bankBranch: string | null;
  bankAccountHolderName: string | null;
  rating: number;
  notes: string | null;
  isActive: boolean;
}

export interface SupplierListItemDto {
  id: number;
  code: string;
  name: string;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  city: string;
  paymentTermsDays: number;
  rating: number;
  isActive: boolean;
}

export interface CreateSupplierRequest {
  code: string | null;
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
  paymentTermsDays: number;
  bankName: string | null;
  bankAccountNumber: string | null;
  bankBranch: string | null;
  bankAccountHolderName: string | null;
  rating: number;
  notes: string | null;
}

export interface UpdateSupplierRequest {
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
  paymentTermsDays: number;
  bankName: string | null;
  bankAccountNumber: string | null;
  bankBranch: string | null;
  bankAccountHolderName: string | null;
  rating: number;
  notes: string | null;
  isActive: boolean;
}
