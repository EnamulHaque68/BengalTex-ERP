import { Component, HostBinding, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-layout',
  standalone: false,
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent implements OnInit, OnDestroy {
  /** Desktop icon-only rail. */
  collapsed = false;
  /** Off-canvas sidebar on mobile. */
  mobileOpen = false;
  /** 'light' | 'dark' — applied as a host class; persisted. */
  theme: 'light' | 'dark' = 'light';

  userName = '';
  userRole = '';

  private readonly COLLAPSE_KEY = 'btx-sidebar-collapsed';
  private readonly THEME_KEY = 'btx-theme';
  private routerSub?: Subscription;

  constructor(private auth: AuthService, private router: Router) {}

  @HostBinding('class.theme-dark') get isDark(): boolean { return this.theme === 'dark'; }

  ngOnInit(): void {
    const user = this.auth.getCurrentUser();
    this.userName = user?.fullName || user?.userName || 'User';
    this.userRole = user?.roles?.[0] || '';

    try {
      this.collapsed = localStorage.getItem(this.COLLAPSE_KEY) === '1';
      const savedTheme = localStorage.getItem(this.THEME_KEY);
      if (savedTheme === 'dark' || savedTheme === 'light') this.theme = savedTheme;
    } catch { /* ignore */ }

    // Close the mobile drawer after an actual navigation (not on group expand/collapse).
    this.routerSub = this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => this.closeMobile());
  }

  ngOnDestroy(): void {
    this.routerSub?.unsubscribe();
  }

  toggleCollapse(): void {
    this.collapsed = !this.collapsed;
    try { localStorage.setItem(this.COLLAPSE_KEY, this.collapsed ? '1' : '0'); } catch { /* ignore */ }
  }

  toggleMobile(): void {
    this.mobileOpen = !this.mobileOpen;
  }

  closeMobile(): void {
    this.mobileOpen = false;
  }

  toggleTheme(): void {
    this.theme = this.theme === 'dark' ? 'light' : 'dark';
    try { localStorage.setItem(this.THEME_KEY, this.theme); } catch { /* ignore */ }
  }

  logout(): void {
    this.auth.logout();
  }
}
