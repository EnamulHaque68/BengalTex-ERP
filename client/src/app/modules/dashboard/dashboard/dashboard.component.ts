import { ChangeDetectorRef, Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { DashboardService } from '../../../services/dashboard.service';
import { DashboardSnapshotDto, NeedsAttentionItemDto } from '../../../models/dashboard.models';

@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  loading = false;
  data: DashboardSnapshotDto | null = null;
  lastRefreshed: Date | null = null;

  private refreshTimer: any = null;

  constructor(private svc: DashboardService, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.load();
    // Auto-refresh every 60s — keeps the snapshot fresh without manual reload
    this.refreshTimer = setInterval(() => this.load(), 60_000);
  }
  ngOnDestroy(): void { if (this.refreshTimer) clearInterval(this.refreshTimer); }

  load(): void {
    this.loading = true;
    this.svc.getSnapshot().subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.data = res.data;
          this.lastRefreshed = new Date();
        }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  // ── Helpers ──
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 0 }).format(amount || 0);
  }
  formatCurrencyShort(amount: number): string {
    if (Math.abs(amount) >= 10_000_000) return `৳${(amount / 10_000_000).toFixed(2)} Cr`;
    if (Math.abs(amount) >= 100_000) return `৳${(amount / 100_000).toFixed(2)} L`;
    if (Math.abs(amount) >= 1_000) return `৳${(amount / 1_000).toFixed(1)} K`;
    return this.formatCurrency(amount);
  }

  severityColor(s: string | null): string {
    return s === 'Critical' ? '#dc2626' : s === 'Warning' ? '#d97706' : '#2563eb';
  }
  severityBg(s: string | null): string {
    return s === 'Critical' ? '#fef2f2' : s === 'Warning' ? '#fffbeb' : '#eff6ff';
  }

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
}
