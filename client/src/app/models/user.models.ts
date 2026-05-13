export interface UserListItemDto {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  factoryId: number | null;
  isActive: boolean;
  isLockedOut: boolean;
  roles: string[];
  createdAt: string;
}

export interface UserDto {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  factoryId: number | null;
  isActive: boolean;
  emailConfirmed: boolean;
  isLockedOut: boolean;
  lockoutEnd: string | null;
  accessFailedCount: number;
  boundDeviceFingerprint: string | null;
  boundDeviceName: string | null;
  deviceBoundAt: string | null;
  createdAt: string;
  createdBy: string | null;
  roles: string[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface PagedQueryParameters {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface CreateUserRequest {
  userName: string;
  email: string;
  fullName: string;
  password: string;
  confirmPassword: string;
  factoryId: number | null;
  roles: string[];
}

export interface UpdateUserRequest {
  userName: string;
  email: string;
  fullName: string;
  factoryId: number | null;
}

export interface RoleListItemDto {
  id: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  memberCount: number;
  permissionCount: number;
}

export interface RoleDto {
  id: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  memberCount: number;
  permissions: string[];
}
