import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { tap, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ApiResponse, AuthData, UserInfo } from '../models/auth.models';
import { TokenStorageService } from './token-storage.service';
import { SignalRService } from './signalr.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/auth`;
  private currentUser$ = new BehaviorSubject<UserInfo | null>(null);

  constructor(
    private http: HttpClient,
    private tokenStorage: TokenStorageService,
    private router: Router,
    private signalR: SignalRService
  ) {
    // Restore user from storage on app init (survives page reload)
    const stored = tokenStorage.getCurrentUser();
    if (stored) {
      this.currentUser$.next(stored);
      // Re-establish the WebSocket so we keep listening for SessionSuperseded
      this.signalR.start();
    }

    // Server tells us our session was replaced from another device
    this.signalR.sessionSuperseded$.subscribe(payload => {
      const mine = this.tokenStorage.getSession();
      // Defensive: if for some reason this client is the new session, ignore
      if (mine?.sessionId === payload.newSessionId) return;

      console.warn('[Auth] Session superseded:', payload.reason);
      this.tokenStorage.clearAll();
      this.currentUser$.next(null);
      this.signalR.stop();
      alert('You have been signed out: your account was used to sign in on another device.');
      this.router.navigate(['/login']);
    });
  }

  get currentUser$Observable(): Observable<UserInfo | null> {
    return this.currentUser$.asObservable();
  }

  get isAuthenticated(): boolean {
    return !!this.tokenStorage.getAccessToken();
  }

  getCurrentUser(): UserInfo | null {
    return this.currentUser$.value;
  }

  login(emailOrUsername: string, password: string, rawFingerprint?: string): Observable<ApiResponse<AuthData>> {
    return this.http.post<ApiResponse<AuthData>>(`${this.apiUrl}/login`, {
      emailOrUsername,
      password,
      rawDeviceFingerprint: rawFingerprint ?? null
    }).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.tokenStorage.saveTokens(res.data);
          this.currentUser$.next({
            userId: res.data.userId,
            userName: res.data.userName,
            email: res.data.email,
            fullName: res.data.fullName,
            factoryId: res.data.factoryId,
            roles: res.data.roles,
            permissions: res.data.permissions
          });
          this.signalR.start();
        }
      })
    );
  }

  logout(): void {
    this.signalR.stop();
    this.http.post(`${this.apiUrl}/logout`, {}).pipe(
      finalize(() => {
        this.tokenStorage.clearAll();
        this.currentUser$.next(null);
        this.router.navigate(['/login']);
      })
    ).subscribe({ error: () => {} });
  }

  refreshToken(): Observable<ApiResponse<AuthData>> {
    const session = this.tokenStorage.getSession();
    const refreshToken = this.tokenStorage.getRefreshToken();

    if (!session || !refreshToken)
      return throwError(() => new Error('No session found.'));

    return this.http.post<ApiResponse<AuthData>>(`${this.apiUrl}/refresh`, {
      userId: session.userId,
      refreshToken: refreshToken,
      sessionId: session.sessionId
    }).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.tokenStorage.saveTokens(res.data);
          this.currentUser$.next({
            userId: res.data.userId,
            userName: res.data.userName,
            email: res.data.email,
            fullName: res.data.fullName,
            factoryId: res.data.factoryId,
            roles: res.data.roles,
            permissions: res.data.permissions
          });
        }
      })
    );
  }

  hasPermission(permission: string): boolean {
    return this.tokenStorage.hasPermission(permission);
  }

  hasRole(role: string): boolean {
    return this.getCurrentUser()?.roles.includes(role) ?? false;
  }

  forgotPassword(email: string): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(
    email: string,
    token: string,
    newPassword: string,
    confirmPassword: string
  ): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${this.apiUrl}/reset-password`, {
      email,
      token,
      newPassword,
      confirmPassword
    });
  }
}
