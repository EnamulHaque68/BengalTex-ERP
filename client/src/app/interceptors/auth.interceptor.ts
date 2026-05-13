import { Injectable } from '@angular/core';
import {
  HttpRequest, HttpHandler, HttpEvent, HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { TokenStorageService } from '../services/token-storage.service';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshSubject = new BehaviorSubject<string | null>(null);

  constructor(
    private tokenStorage: TokenStorageService,
    private authService: AuthService
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Skip token injection for auth endpoints
    if (this.isAuthEndpoint(req.url))
      return next.handle(req);

    const token = this.tokenStorage.getAccessToken();
    const authReq = token ? this.addBearer(req, token) : req;

    return next.handle(authReq).pipe(
      catchError(err => {
        if (err instanceof HttpErrorResponse && err.status === 401)
          return this.handle401(req, next);
        return throwError(() => err);
      })
    );
  }

  private handle401(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (this.isRefreshing) {
      // Wait for the in-progress refresh to complete, then retry with new token
      return this.refreshSubject.pipe(
        filter(token => token !== null),
        take(1),
        switchMap(token => next.handle(this.addBearer(req, token!)))
      );
    }

    this.isRefreshing = true;
    this.refreshSubject.next(null);

    return this.authService.refreshToken().pipe(
      switchMap(() => {
        this.isRefreshing = false;
        const newToken = this.tokenStorage.getAccessToken()!;
        this.refreshSubject.next(newToken);
        return next.handle(this.addBearer(req, newToken));
      }),
      catchError(err => {
        this.isRefreshing = false;
        this.tokenStorage.clearAll();
        window.location.href = '/login';
        return throwError(() => err);
      })
    );
  }

  private addBearer(req: HttpRequest<any>, token: string): HttpRequest<any> {
    return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  private isAuthEndpoint(url: string): boolean {
    return url.includes('/auth/login') || url.includes('/auth/refresh');
  }
}
