export interface ApiResponse<T = void> {
  success: boolean;
  data?: T;
  message?: string;
  errors: ApiError[];
  traceId?: string;
}

export interface ApiError {
  code: string;
  message: string;
  field?: string;
}

export interface AuthData {
  accessToken: string;
  refreshToken: string;
  sessionId: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  userId: string;
  userName: string;
  email: string;
  fullName: string;
  factoryId?: number;
  roles: string[];
  permissions: string[];
}

export interface UserInfo {
  userId: string;
  userName: string;
  email: string;
  fullName: string;
  factoryId?: number;
  roles: string[];
  permissions: string[];
}

export interface LoginRequest {
  emailOrUsername: string;
  password: string;
  rawDeviceFingerprint?: string;
}

export interface RefreshRequest {
  userId: string;
  refreshToken: string;
  sessionId: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}
