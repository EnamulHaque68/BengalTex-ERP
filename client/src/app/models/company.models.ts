export interface CompanyDto {
  id: number;
  name: string;
  shortName: string;
  registrationNumber: string | null;
  taxNumber: string | null;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  district: string;
  postalCode: string | null;
  country: string;
  phone: string | null;
  email: string | null;
  website: string | null;
  logoUrl: string | null;
  isActive: boolean;
}

export interface FactoryDto {
  id: number;
  name: string;
  code: string;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  district: string | null;
  postalCode: string | null;
  phone: string | null;
  isActive: boolean;
  geoFenceLat: number | null;
  geoFenceLng: number | null;
  geoFenceRadiusMeters: number | null;
  companyId: number;
}

export interface FactoryListItemDto {
  id: number;
  name: string;
  code: string;
  city: string;
  phone: string | null;
  isActive: boolean;
  hasGeoFence: boolean;
}
