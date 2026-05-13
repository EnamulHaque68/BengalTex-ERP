import { Injectable } from '@angular/core';
import { AuthData, UserInfo } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private readonly AT_KEY = 'btx_at';
  private readonly RT_KEY = 'btx_rt';
  private readonly SESSION_KEY = 'btx_sess';
  private readonly USER_KEY = 'btx_user';

  saveTokens(data: AuthData): void {
    sessionStorage.setItem(this.AT_KEY, data.accessToken);
    localStorage.setItem(this.RT_KEY, data.refreshToken);
    localStorage.setItem(this.SESSION_KEY, JSON.stringify({
      userId: data.userId,
      sessionId: data.sessionId
    }));
    localStorage.setItem(this.USER_KEY, JSON.stringify({
      userId: data.userId,
      userName: data.userName,
      email: data.email,
      fullName: data.fullName,
      factoryId: data.factoryId,
      roles: data.roles,
      permissions: data.permissions
    } as UserInfo));
  }

  getAccessToken(): string | null {
    return sessionStorage.getItem(this.AT_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.RT_KEY);
  }

  getSession(): { userId: string; sessionId: string } | null {
    const raw = localStorage.getItem(this.SESSION_KEY);
    return raw ? JSON.parse(raw) : null;
  }

  getCurrentUser(): UserInfo | null {
    const raw = localStorage.getItem(this.USER_KEY);
    return raw ? JSON.parse(raw) : null;
  }

  clearAll(): void {
    sessionStorage.removeItem(this.AT_KEY);
    localStorage.removeItem(this.RT_KEY);
    localStorage.removeItem(this.SESSION_KEY);
    localStorage.removeItem(this.USER_KEY);
  }

  isAuthenticated(): boolean {
    // Access token exists in sessionStorage OR refresh token for auto-refresh
    return !!this.getAccessToken() || !!this.getRefreshToken();
  }

  hasPermission(permission: string): boolean {
    return this.getCurrentUser()?.permissions.includes(permission) ?? false;
  }
}
