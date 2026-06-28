import { ChangeDetectorRef, Component, HostBinding, NgZone, OnDestroy, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter, interval } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { AvatarRefreshService } from '../services/avatar-refresh.service';
import { NotificationService } from '../services/notification.service';
import { SignalRService } from '../services/signalr.service';
import { environment } from '../../environments/environment';

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

  /** Public company-logo endpoint; shown in the sidebar brand, falls back to the bolt icon on 404. */
  readonly companyLogoUrl = `${environment.apiBaseUrl}/api/company/logo`;
  logoOk = true;

  // Topbar user avatar + dropdown
  avatarUrl: SafeUrl | null = null;
  userMenuOpen = false;
  private avatarObjectUrl: string | null = null;

  /** Unread notifications since the user last opened the notifications page. */
  unreadCount = 0;

  private readonly COLLAPSE_KEY = 'btx-sidebar-collapsed';
  private readonly THEME_KEY = 'btx-theme';
  private readonly NOTIF_SEEN_KEY = 'btx-notif-last-seen';
  private readonly NOTIF_POLL_MS = 60000;   // fallback poll; SignalR is the primary trigger
  private routerSub?: Subscription;
  private avatarSub?: Subscription;
  private notifSub?: Subscription;
  private notifPollSub?: Subscription;

  constructor(
    private auth: AuthService,
    private router: Router,
    private http: HttpClient,
    private sanitizer: DomSanitizer,
    private avatarRefresh: AvatarRefreshService,
    private notifications: NotificationService,
    private signalr: SignalRService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  private get notifLastSeen(): string | null {
    try { return localStorage.getItem(this.NOTIF_SEEN_KEY); } catch { return null; }
  }

  /** Re-read the authoritative unread count from the server (since last-seen). */
  private refreshUnreadCount(): void {
    this.notifications.getUnreadCount(this.notifLastSeen).subscribe({
      next: (res) => this.zone.run(() => {
        this.unreadCount = res.success ? (res.data ?? 0) : 0;
        this.cdr.detectChanges();
      }),
      error: () => { /* keep last known count; the next poll retries */ }
    });
  }

  /** Mark all current notifications as seen (called when the user opens the notifications page). */
  private markNotificationsSeen(): void {
    try { localStorage.setItem(this.NOTIF_SEEN_KEY, new Date().toISOString()); } catch { /* ignore */ }
    this.unreadCount = 0;
    this.cdr.detectChanges();
  }

  /** Initials fallback when there's no photo (e.g. "Operator One" → "OO"). */
  get initials(): string {
    const n = (this.userName || '').trim();
    if (!n) return 'U';
    const parts = n.split(/\s+/).filter(Boolean);
    return ((parts[0]?.[0] ?? '') + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase() || 'U';
  }

  toggleUserMenu(): void { this.userMenuOpen = !this.userMenuOpen; }
  closeUserMenu(): void { this.userMenuOpen = false; }

  private loadAvatar(): void {
    // Cache-bust so a freshly-uploaded photo isn't served stale from the browser cache.
    this.http.get(`${environment.apiBaseUrl}/api/employees/my-photo?v=${Date.now()}`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        if (this.avatarObjectUrl) URL.revokeObjectURL(this.avatarObjectUrl);
        this.avatarObjectUrl = URL.createObjectURL(blob);
        this.avatarUrl = this.sanitizer.bypassSecurityTrustUrl(this.avatarObjectUrl);
      },
      error: () => {
        if (this.avatarObjectUrl) { URL.revokeObjectURL(this.avatarObjectUrl); this.avatarObjectUrl = null; }
        this.avatarUrl = null;   // no photo / not linked → initials fallback
      }
    });
  }

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

    this.loadAvatar();
    // Refresh the topbar avatar the instant the user changes their photo (e.g. on My Profile).
    this.avatarSub = this.avatarRefresh.changes$.subscribe(() => this.loadAvatar());

    // Close the mobile drawer + user menu after an actual navigation (not on group expand/collapse).
    // Also reset the bell badge whenever the user lands on the notifications page.
    this.routerSub = this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe((e) => {
        this.closeMobile();
        this.closeUserMenu();
        if ((e as NavigationEnd).urlAfterRedirects.startsWith('/notifications')) {
          this.markNotificationsSeen();
        }
      });

    // ── Notification bell badge ──
    // If we're already on the notifications page at load, treat everything as seen.
    if (this.router.url.startsWith('/notifications')) this.markNotificationsSeen();
    else this.refreshUnreadCount();

    // Primary trigger: SignalR push whenever any notification is created.
    this.notifSub = this.signalr.notificationReceived$.subscribe(() => {
      if (!this.router.url.startsWith('/notifications')) this.refreshUnreadCount();
    });

    // Fallback: poll periodically in case the socket is down.
    this.notifPollSub = interval(this.NOTIF_POLL_MS).subscribe(() => {
      if (!this.router.url.startsWith('/notifications')) this.refreshUnreadCount();
    });
  }

  ngOnDestroy(): void {
    this.routerSub?.unsubscribe();
    this.avatarSub?.unsubscribe();
    this.notifSub?.unsubscribe();
    this.notifPollSub?.unsubscribe();
    if (this.avatarObjectUrl) URL.revokeObjectURL(this.avatarObjectUrl);
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
