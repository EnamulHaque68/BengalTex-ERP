export interface PermissionGroupDto {
  category: string;
  permissions: PermissionItemDto[];
}

export interface PermissionItemDto {
  key: string;     // "Customers.View"
  action: string;  // "View"
}
