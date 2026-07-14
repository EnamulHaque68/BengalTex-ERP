import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FinancialIntelligenceService } from '../../../services/financial-intelligence.service';
import { FinancialKpisDto, ArApAgingDto, ProfitTrendPointDto } from '../../../models/financial-intelligence.models';

/**
 * Phase A8 — Financial Intelligence. Liquidity / profitability / efficiency / leverage ratios,
 * AR/AP aging, and the monthly P&L trend — all read-only over the posted GL + open invoices.
 */
@Component({
  selector: 'app-financial-intelligence',
  standalone: false,
  templateUrl: './financial-intelligence.component.html',
  styleUrl: './financial-intelligence.component.scss'
})
export class FinancialIntelligenceComponent implements OnInit {
  loading = false;
  asOf = '';
  fromDate = '';
  toDate = '';

  kpis: FinancialKpisDto | null = null;
  aging: ArApAgingDto | null = null;
  trend: ProfitTrendPointDto[] = [];
  trendMax = 1;

  constructor(
    private svc: FinancialIntelligenceService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {
    const now = new Date();
    this.asOf = now.toISOString().slice(0, 10);
    this.fromDate = new Date(now.getFullYear(), 0, 1).toISOString().slice(0, 10);
    this.toDate = this.asOf;
  }

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.svc.kpis(this.asOf, this.fromDate, this.toDate).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.kpis = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
    this.svc.aging(this.asOf).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.aging = res.data; this.cdr.detectChanges(); })
    });
    this.svc.profitTrend(12).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          this.trend = res.data.points;
          this.trendMax = Math.max(1, ...this.trend.map(p => Math.max(Math.abs(p.revenue), Math.abs(p.expense))));
        }
        this.cdr.detectChanges();
      })
    });
  }

  barHeight(v: number): number { return Math.round(Math.abs(v) / this.trendMax * 100); }

  ratioClass(v: number, good: number, warn: number): string {
    return v >= good ? 'good' : v >= warn ? 'warn' : 'bad';
  }

  formatMoney(v: number | undefined): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 0 }).format(v || 0);
  }
  formatNum(v: number | undefined, dp = 2): string {
    return (v ?? 0).toLocaleString('en-US', { minimumFractionDigits: dp, maximumFractionDigits: dp });
  }
}
