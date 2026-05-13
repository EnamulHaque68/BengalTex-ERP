import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { TokenStorageService } from '../services/token-storage.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(private tokenStorage: TokenStorageService, private router: Router) {}

  canActivate(): boolean {
    if (this.tokenStorage.isAuthenticated())
      return true;

    this.router.navigate(['/login']);
    return false;
  }
}
