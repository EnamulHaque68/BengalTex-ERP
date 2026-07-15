import { ChangeDetectorRef, Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { DashboardService } from '../../../services/dashboard.service';
import { FinancialIntelligenceService } from '../../../services/financial-intelligence.service';
import { FactoryService } from '../../../services/company.service';
import { AuthService } from '../../../services/auth.service';
import { DashboardSnapshotDto, NeedsAttentionItemDto, ExpenseBreakdownItemDto, ProductionOverviewDto } from '../../../models/dashboard.models';
import { ProfitTrendPointDto } from '../../../models/financial-intelligence.models';

interface WidgetCfg { id: string; label: string; visible: boolean; }

const DEFAULT_WIDGETS: WidgetCfg[] = [
  { id: 'today', label: 'Today Sales / Purchase / Expenses', visible: true },
  { id: 'financials', label: 'Receivable / Payable / Cash', visible: true },
  { id: 'operational', label: 'Operational KPIs', visible: true },
  { id: 'revExp', label: 'Revenue vs Expense chart', visible: true },
  { id: 'expenseDonut', label: 'Expense Breakdown', visible: true },
  { id: 'lowStock', label: 'Low Stock Alert', visible: true },
  { id: 'production', label: 'Production Overview', visible: true },
  { id: 'recentOrders', label: 'Recent Production Orders', visible: true },
  { id: 'upcomingSalary', label: 'Upcoming Salary', visible: true },
  { id: 'needsAttention', label: 'Needs Your Attention', visible: true },
  { id: 'attendance', label: 'Today Attendance', visible: true },
];

@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  loading = false;
  error = false;
  data: DashboardSnapshotDto | null = null;
  trend: ProfitTrendPointDto[] = [];
  trendError = false;
  lastRefreshed: Date | null = null;
  userName = 'there';

  trendMonths = 6;
  readonly trendOptions = [
    { label: 'Last 6 Months', value: 6 },
    { label: 'Last 12 Months', value: 12 },
  ];

  // ── Period / date-range filters ──
  readonly periodOptions = [
    { label: 'This Month', value: 'ThisMonth' },
    { label: 'Last Month', value: 'LastMonth' },
    { label: 'This Quarter', value: 'ThisQuarter' },
    { label: 'This Year', value: 'ThisYear' },
    { label: 'Custom', value: 'Custom' },
  ];
  globalPreset = 'ThisMonth';
  customFrom = ''; customTo = '';
  expensePreset = 'ThisMonth';
  prodPreset = 'ThisMonth';

  // Period-filtered widget data (default from the snapshot's This-Month values)
  expenseItems: ExpenseBreakdownItemDto[] = [];
  prodOverride: ProductionOverviewDto | null = null;

  // Factory picker (real list). Data isn't factory-scoped yet, so this is informational — see note.
  factories: { id: number; name: string }[] = [];
  selectedFactory: number | null = null;

  // Charts
  revExpData: any = null; revExpOptions: any = null;
  expenseData: any = null; donutOptions: any = null;
  attendanceData: any = null;
  readonly expenseColors = ['#3b82f6', '#ef4444', '#f59e0b', '#8b5cf6', '#10b981', '#06b6d4', '#94a3b8'];

  // Customize
  widgets: WidgetCfg[] = [];
  customizeVisible = false;

  private refreshTimer: any = null;

  constructor(
    private svc: DashboardService,
    private fi: FinancialIntelligenceService,
    private factorySvc: FactoryService,
    private auth: AuthService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  private userId = 'anon';

  ngOnInit(): void {
    const u = this.auth.getCurrentUser();
    this.userName = u?.fullName || u?.userName || 'there';
    this.userId = u?.userId || 'anon';
    const r = this.rangeFor('ThisMonth');
    this.customFrom = r.from; this.customTo = r.to;
    this.buildDonutOptions();
    this.loadWidgets();
    this.load();
    this.factorySvc.getAll(false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.factories = res.data.map((f: any) => ({ id: f.id, name: f.name }));
        this.cdr.detectChanges();
      })
    });
    this.refreshTimer = setInterval(() => this.load(true), 60_000);
  }

  // ── Period range helpers ──
  private iso(d: Date): string { return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; }
  rangeFor(preset: string): { from: string; to: string } {
    const now = new Date(); const y = now.getFullYear(), m = now.getMonth();
    switch (preset) {
      case 'LastMonth': return { from: this.iso(new Date(y, m - 1, 1)), to: this.iso(new Date(y, m, 0)) };
      case 'ThisQuarter': return { from: this.iso(new Date(y, Math.floor(m / 3) * 3, 1)), to: this.iso(now) };
      case 'ThisYear': return { from: this.iso(new Date(y, 0, 1)), to: this.iso(now) };
      case 'Custom': return { from: this.customFrom, to: this.customTo };
      default: return { from: this.iso(new Date(y, m, 1)), to: this.iso(now) };   // ThisMonth
    }
  }

  /** Header date-range → drives both period widgets. */
  onGlobalRange(): void {
    if (this.globalPreset === 'Custom' && (!this.customFrom || !this.customTo)) return;
    this.expensePreset = this.prodPreset = this.globalPreset;
    const r = this.rangeFor(this.globalPreset);
    this.fetchExpense(r.from, r.to);
    this.fetchProd(r.from, r.to);
  }

  onExpensePeriod(): void { const r = this.rangeFor(this.expensePreset); this.fetchExpense(r.from, r.to); }
  onProdPeriod(): void { const r = this.rangeFor(this.prodPreset); this.fetchProd(r.from, r.to); }

  private fetchExpense(from: string, to: string): void {
    if (!from || !to) return;
    this.svc.expenseBreakdown(from, to).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) { this.expenseItems = res.data; this.buildExpenseChart(); } this.cdr.detectChanges(); })
    });
  }
  private fetchProd(from: string, to: string): void {
    if (!from || !to) return;
    this.svc.productionOverview(from, to).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.prodOverride = res.data; this.cdr.detectChanges(); })
    });
  }

  get prodOverview(): ProductionOverviewDto | null { return this.prodOverride ?? this.data?.production?.overview ?? null; }
  ngOnDestroy(): void { if (this.refreshTimer) clearInterval(this.refreshTimer); }

  // ── Data load ──
  load(silent = false): void {
    if (!silent) { this.loading = true; this.error = false; }
    forkJoin({
      snap: this.svc.getSnapshot(),
      trend: this.fi.profitTrend(this.trendMonths)
    }).subscribe({
      next: (r) => this.zone.run(() => {
        this.loading = false;
        if (r.snap.success && r.snap.data) {
          this.data = r.snap.data;
          // Keep the user's chosen period across silent refreshes; only sync when on the default.
          if (this.expensePreset === 'ThisMonth') this.expenseItems = this.data.expenseBreakdown ?? [];
          this.buildCharts();
        }
        else this.error = true;
        this.trendError = !(r.trend.success && r.trend.data);
        if (!this.trendError) { this.trend = r.trend.data!.points; this.buildRevExpChart(); }
        this.lastRefreshed = new Date();
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; if (!silent) this.error = true; this.cdr.detectChanges(); })
    });
  }
  reloadTrend(): void {
    this.fi.profitTrend(this.trendMonths).subscribe({
      next: (r) => this.zone.run(() => {
        this.trendError = !(r.success && r.data);
        if (!this.trendError) { this.trend = r.data!.points; this.buildRevExpChart(); }
        this.cdr.detectChanges();
      })
    });
  }

  // ── Charts ──
  private textColor(): string { return getComputedStyle(document.documentElement).getPropertyValue('--text-color') || '#334155'; }

  private buildRevExpChart(): void {
    const labels = this.trend.map(p => `${p.label} ${String(p.year).slice(2)}`);
    this.revExpData = {
      labels,
      datasets: [
        { type: 'bar', label: 'Revenue', backgroundColor: '#3b82f6', borderRadius: 4, data: this.trend.map(p => p.revenue), order: 2 },
        { type: 'bar', label: 'Expense', backgroundColor: '#f59e0b', borderRadius: 4, data: this.trend.map(p => p.expense), order: 2 },
        { type: 'line', label: 'Net Profit', borderColor: '#10b981', backgroundColor: '#10b981', tension: 0.35, fill: false, data: this.trend.map(p => p.netProfit), order: 1 },
      ]
    };
    const fmt = (v: number) => this.formatShort(v);
    this.revExpOptions = {
      maintainAspectRatio: false, responsive: true,
      plugins: {
        legend: { position: 'top', labels: { usePointStyle: true, boxWidth: 8, font: { size: 11 } } },
        tooltip: { callbacks: { label: (c: any) => `${c.dataset.label}: ${this.formatCurrency(c.parsed.y)}` } }
      },
      scales: {
        x: { grid: { display: false }, ticks: { font: { size: 10 } } },
        y: { grid: { color: 'rgba(148,163,184,.15)' }, ticks: { font: { size: 10 }, callback: (v: any) => fmt(v) } }
      }
    };
  }

  private buildDonutOptions(): void {
    // Both donuts render their own legend list beside the ring (reference style) → hide chart legend.
    this.donutOptions = {
      maintainAspectRatio: false, responsive: true, cutout: '72%',
      plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: (c: any) => `${c.label}: ${this.formatCurrency(c.parsed)}` } }
      }
    };
  }

  expensePct(amount: number): number {
    const t = this.expenseTotal;
    return t > 0 ? Math.round(amount / t * 1000) / 10 : 0;
  }

  private buildExpenseChart(): void {
    const eb = this.expenseItems;
    this.expenseData = eb.length
      ? { labels: eb.map(x => x.name), datasets: [{ data: eb.map(x => x.amount), backgroundColor: this.expenseColors, borderWidth: 0 }] }
      : null;
  }

  private buildCharts(): void {
    this.buildExpenseChart();

    // Attendance donut — when there's no attendance yet (all zero) show a soft grey ring
    // so the widget reads as "empty" instead of a blank centre.
    const at = this.data?.hr?.attendance;
    if (at) {
      const sum = at.present + at.late + at.onLeave + at.absent;
      this.attendanceData = sum > 0
        ? { labels: ['Present', 'Late', 'On Leave', 'Absent'],
            datasets: [{ data: [at.present, at.late, at.onLeave, at.absent], backgroundColor: ['#10b981', '#f59e0b', '#3b82f6', '#ef4444'], borderWidth: 0 }] }
        : { labels: ['No attendance yet'],
            datasets: [{ data: [1], backgroundColor: ['#e5e7eb'], borderWidth: 0 }] };
    } else this.attendanceData = null;
  }

  get expenseTotal(): number { return this.expenseItems.reduce((a, b) => a + b.amount, 0); }

  // ── Customize (localStorage) ──
  private storageKey(): string { return `btx.dashboard.widgets.${this.userId}`; }
  private loadWidgets(): void {
    try {
      const raw = localStorage.getItem(this.storageKey());
      if (raw) {
        const saved: WidgetCfg[] = JSON.parse(raw);
        // Merge: keep saved order/visibility, append any new default widgets not yet saved.
        const byId = new Map(saved.map(w => [w.id, w]));
        this.widgets = [
          ...saved.filter(w => DEFAULT_WIDGETS.some(d => d.id === w.id)),
          ...DEFAULT_WIDGETS.filter(d => !byId.has(d.id))
        ];
        return;
      }
    } catch { /* fall through to defaults */ }
    this.widgets = DEFAULT_WIDGETS.map(w => ({ ...w }));
  }
  private saveWidgets(): void { try { localStorage.setItem(this.storageKey(), JSON.stringify(this.widgets)); } catch { /* ignore */ } }

  /** A widget renders only when it is visible AND its data is present (backend permission-gated). */
  shows(id: string): boolean {
    const w = this.widgets.find(x => x.id === id);
    if (!w || !w.visible) return false;
    return this.available(id);
  }
  available(id: string): boolean {
    const d = this.data;
    switch (id) {
      case 'today': return !!d?.todayKpis;
      case 'financials': return !!d;
      case 'operational': return !!d;
      case 'revExp': return !this.trendError && this.trend.length > 0;
      case 'expenseDonut': return !!d?.expenseBreakdown?.length;
      case 'lowStock': return !!d?.lowStock;
      case 'production': return !!d?.production;
      case 'recentOrders': return !!d?.production?.recentOrders?.length;
      case 'upcomingSalary': return !!d?.hr?.upcomingSalary;
      case 'needsAttention': return !!d;
      case 'attendance': return !!d?.hr?.attendance;
      default: return true;
    }
  }
  /** Widgets available to this user (for the customize list). */
  get customizableWidgets(): WidgetCfg[] { return this.widgets.filter(w => this.available(w.id)); }

  toggleWidget(w: WidgetCfg): void { w.visible = !w.visible; this.saveWidgets(); }
  moveWidget(w: WidgetCfg, dir: -1 | 1): void {
    const i = this.widgets.indexOf(w); const j = i + dir;
    if (j < 0 || j >= this.widgets.length) return;
    [this.widgets[i], this.widgets[j]] = [this.widgets[j], this.widgets[i]];
    this.saveWidgets();
  }
  resetLayout(): void { this.widgets = DEFAULT_WIDGETS.map(w => ({ ...w })); this.saveWidgets(); }

  /** Grid column span per widget (12-col grid) — mirrors the reference row structure. */
  spanClass(id: string): string {
    switch (id) {
      case 'operational': return 'span-12';
      case 'today': case 'financials': return 'span-6';        // 3 + 3 = one 6-card row
      case 'revExp': return 'span-6';                          // chart row: 6 + 3 + 3
      case 'expenseDonut': case 'lowStock': return 'span-3';
      case 'needsAttention': return 'span-8';                  // bottom row: 8 + 4
      case 'production': case 'recentOrders': case 'upcomingSalary':
      case 'attendance': return 'span-4';
      default: return 'span-12';
    }
  }

  // ── Drill-down ──
  private readonly routes: Record<string, string> = {
    todaySales: '/customer-invoices', receivable: '/customer-invoices', overdue: '/customer-invoices',
    todayPurchase: '/supplier-invoices', payable: '/supplier-invoices',
    todayExpenses: '/accounting/profit-loss', cash: '/accounting/bank-book',
    employees: '/attendance', rmStock: '/stock-on-hand', fgStock: '/stock-on-hand', lowStock: '/stock-on-hand',
    production: '/production-orders', orders: '/sales-orders', salary: '/payroll', attendance: '/attendance',
  };
  routeFor(key: string): string { return this.routes[key] || '/'; }

  routeForItem(item: NeedsAttentionItemDto): string {
    switch (item.type) {
      case 'ExpiringCert': return '/compliance/certificates';
      case 'OverdueCap': return '/compliance/audits';
      case 'PendingLeave': return '/leaves';
      case 'UnreconciledStmt': return '/bank-reconciliation';
      case 'OverdueInvoice': return '/customer-invoices';
      default: return '/';
    }
  }

  // ── Helpers ──
  pctChange(cur: number, prev: number): number | null {
    if (prev === 0) return cur === 0 ? 0 : null;
    return Math.round((cur - prev) / Math.abs(prev) * 1000) / 10;
  }
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 0 }).format(amount || 0);
  }
  formatShort(amount: number): string {
    const a = Math.abs(amount || 0);
    if (a >= 1e7) return `৳${(amount / 1e7).toFixed(2)} Cr`;
    if (a >= 1e5) return `৳${(amount / 1e5).toFixed(2)} L`;
    if (a >= 1e3) return `৳${(amount / 1e3).toFixed(1)}K`;
    return this.formatCurrency(amount);
  }
  // ── Smooth area sparkline (dependency-free SVG; real data only) ──
  // viewBox is 0..100 × 0..40, stretched to the card width via preserveAspectRatio="none".
  private sparkPts(vals: number[]): { x: number; y: number }[] {
    const W = 100, H = 40, padT = 8, padB = 6;
    const max = Math.max(...vals, 0), min = Math.min(...vals, 0);
    const range = (max - min) || 1;
    const n = vals.length;
    return vals.map((v, i) => ({
      x: n > 1 ? (i / (n - 1)) * W : W / 2,
      y: padT + (1 - (v - min) / range) * (H - padT - padB)
    }));
  }
  /** Catmull-Rom → cubic-bezier smoothing for a soft, natural curve. */
  private smoothPath(pts: { x: number; y: number }[]): string {
    if (!pts.length) return '';
    if (pts.length === 1) return `M0,${pts[0].y} L100,${pts[0].y}`;
    let d = `M${pts[0].x.toFixed(2)},${pts[0].y.toFixed(2)}`;
    for (let i = 0; i < pts.length - 1; i++) {
      const p0 = pts[i - 1] || pts[i], p1 = pts[i], p2 = pts[i + 1], p3 = pts[i + 2] || pts[i + 1];
      const c1x = p1.x + (p2.x - p0.x) / 6, c1y = p1.y + (p2.y - p0.y) / 6;
      const c2x = p2.x - (p3.x - p1.x) / 6, c2y = p2.y - (p3.y - p1.y) / 6;
      d += ` C${c1x.toFixed(2)},${c1y.toFixed(2)} ${c2x.toFixed(2)},${c2y.toFixed(2)} ${p2.x.toFixed(2)},${p2.y.toFixed(2)}`;
    }
    return d;
  }
  sparkLine(vals: number[]): string { return vals?.length ? this.smoothPath(this.sparkPts(vals)) : ''; }
  sparkArea(vals: number[]): string {
    const line = this.sparkLine(vals);
    return line ? `${line} L100,40 L0,40 Z` : '';
  }
  sevClass(s: string | null): string { return s === 'Critical' ? 'sev-critical' : s === 'Warning' ? 'sev-warning' : 'sev-info'; }
  statusClass(s: string): string {
    const l = (s || '').toLowerCase();
    if (l.includes('complet')) return 'st-completed';
    if (l.includes('progress')) return 'st-progress';
    if (l.includes('delay') || l.includes('overdue')) return 'st-delayed';
    if (l.includes('hold')) return 'st-hold';
    return 'st-normal';
  }
}
