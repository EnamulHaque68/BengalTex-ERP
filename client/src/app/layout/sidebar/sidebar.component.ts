import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { NAV_MENU, NavItem } from '../nav-menu.config';

@Component({
  selector: 'app-sidebar',
  standalone: false,
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SidebarComponent implements OnInit, OnDestroy {
  /** Icon-only rail mode (toggled from the layout topbar). */
  @Input() collapsed = false;

  readonly menu = NAV_MENU;
  /** Titles of currently-expanded groups. */
  openGroups = new Set<string>();
  currentUrl = '';

  /** Menu search term (Phase 4). When non-empty the sidebar shows flat breadcrumb results. */
  searchTerm = '';

  /** The single best (longest-prefix) menu route matching the current URL — drives active state. */
  private bestMatchRoute = '';

  private readonly STORAGE_KEY = 'btx-sidebar-open-groups';
  private routerSub?: Subscription;

  constructor(
    private auth: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Restore which groups the user had open (persists across refresh).
    try {
      const saved = JSON.parse(localStorage.getItem(this.STORAGE_KEY) || '[]') as string[];
      saved.forEach(g => this.openGroups.add(g));
    } catch { /* ignore corrupt storage */ }

    this.currentUrl = this.router.url;
    this.computeBestMatch();
    this.expandActiveGroup();

    // Keep active state + active-group expansion in sync with navigation (and after refresh).
    this.routerSub = this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => {
        this.currentUrl = this.router.url;
        this.computeBestMatch();
        this.expandActiveGroup();
        this.cdr.markForCheck();
      });
  }

  ngOnDestroy(): void {
    this.routerSub?.unsubscribe();
  }

  // ── Role-based visibility (Phase 2) ─────────────────────────────────────
  /** A leaf is visible when it needs no permission, the user has it, or the user is SuperAdmin. */
  isLeafVisible(item: NavItem): boolean {
    if (!item.permission) return true;
    return this.auth.hasRole('SuperAdmin') || this.auth.hasPermission(item.permission);
  }

  /** A group is visible only when at least one of its children is visible. */
  isGroupVisible(group: NavItem): boolean {
    return (group.children ?? []).some(c => this.isLeafVisible(c));
  }

  /** Top-level item: leaf → leaf rule; group → group rule. */
  isVisible(item: NavItem): boolean {
    return item.children?.length ? this.isGroupVisible(item) : this.isLeafVisible(item);
  }

  // ── Expand / collapse ───────────────────────────────────────────────────
  toggleGroup(group: NavItem): void {
    if (this.openGroups.has(group.title)) this.openGroups.delete(group.title);
    else this.openGroups.add(group.title);
    this.persist();
  }

  isOpen(group: NavItem): boolean {
    return this.openGroups.has(group.title);
  }

  // ── Active highlight (Phase 3) ──────────────────────────────────────────
  /**
   * Active = this item owns the single best (longest) menu route matching the URL. Longest-match
   * so that on '/expenses/categories' only "Expense Categories" lights up, not also "Expenses";
   * and on '/customer-invoices/5/print' the "Customer Invoices" feature still highlights.
   */
  isLeafActive(item: NavItem): boolean {
    return !!item.route && item.route === this.bestMatchRoute;
  }

  /** Find the longest menu route that equals or is a parent path of the current URL. */
  private computeBestMatch(): void {
    const url = (this.currentUrl || '/').split('?')[0].split('#')[0];
    let best = '';
    const consider = (route?: string) => {
      if (!route) return;
      const match = route === '/' ? url === '/' : (url === route || url.startsWith(route + '/'));
      if (match && route.length > best.length) best = route;
    };
    for (const item of this.menu) {
      if (item.children?.length) item.children.forEach(c => consider(c.route));
      else consider(item.route);
    }
    this.bestMatchRoute = best;
  }

  /** Parent group is highlighted when any of its children is the active route. */
  isGroupActive(group: NavItem): boolean {
    return (group.children ?? []).some(c => this.isLeafActive(c));
  }

  /** Auto-open the group that contains the active route (so a deep-link/refresh reveals it). */
  private expandActiveGroup(): void {
    for (const item of this.menu) {
      if (item.children?.length && this.isGroupActive(item)) {
        this.openGroups.add(item.title);
      }
    }
  }

  private persist(): void {
    try { localStorage.setItem(this.STORAGE_KEY, JSON.stringify([...this.openGroups])); } catch { /* ignore */ }
  }

  trackByTitle(_i: number, item: NavItem): string {
    return item.title;
  }

  // ── Menu search (Phase 4) ───────────────────────────────────────────────
  get isSearching(): boolean {
    return this.searchTerm.trim().length > 0;
  }

  /**
   * Flat, visible leaf matches for the current term — matched on the leaf title OR its
   * parent group title (so typing "ledger" finds Accounts › General Ledger, and typing
   * "sales" lists everything under Sales).
   */
  get searchResults(): { route: string; title: string; icon?: string; breadcrumb: string }[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return [];

    const out: { route: string; title: string; icon?: string; breadcrumb: string }[] = [];
    for (const item of this.menu) {
      if (item.children?.length) {
        const groupHit = item.title.toLowerCase().includes(term);
        for (const child of item.children) {
          if (!child.route || !this.isLeafVisible(child)) continue;
          if (groupHit || child.title.toLowerCase().includes(term)) {
            out.push({ route: child.route, title: child.title, icon: child.icon, breadcrumb: item.title });
          }
        }
      } else if (item.route && this.isLeafVisible(item) && item.title.toLowerCase().includes(term)) {
        out.push({ route: item.route, title: item.title, icon: item.icon, breadcrumb: '' });
      }
    }
    return out;
  }

  clearSearch(): void {
    this.searchTerm = '';
  }
}
