import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService, ProfitabilityReportDto, CostCenterStatementDto } from '../../../services/accounting.service';

type Tab = 'buyer' | 'style' | 'order' | 'cost-center';

/** Phase A3 — dimensional profitability: buyer / style / order gross margin + cost-center statement. */
@Component({
  selector: 'app-profitability',
  standalone: false,
  templateUrl: './profitability.component.html',
  styleUrl: './profitability.component.scss'
})
export class ProfitabilityComponent implements OnInit {
  tab: Tab = 'buyer';
  fromDate = '';
  toDate = '';
  loading = false;
  report: ProfitabilityReportDto | null = null;
  ccStatement: CostCenterStatementDto | null = null;

  readonly tabs: { key: Tab; label: string }[] = [
    { key: 'buyer', label: 'By Buyer' },
    { key: 'style', label: 'By Style' },
    { key: 'order', label: 'By Order' },
    { key: 'cost-center', label: 'Cost Centers' }
  ];

  constructor(private svc: AccountingService, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    const now = new Date();
    this.fromDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
    this.toDate = now.toISOString().slice(0, 10);
    this.load();
  }

  setTab(t: Tab): void { this.tab = t; this.load(); }

  load(): void {
    this.loading = true;
    this.report = null; this.ccStatement = null;
    if (this.tab === 'cost-center') {
      this.svc.costCenterStatement(this.fromDate, this.toDate).subscribe({
        next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.ccStatement = res.data; this.cdr.detectChanges(); }),
        error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
      });
    } else {
      this.svc.profitability(this.tab, this.fromDate, this.toDate).subscribe({
        next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.report = res.data; this.cdr.detectChanges(); }),
        error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
      });
    }
  }

  marginClass(m: number): string { return m >= 25 ? 'm-good' : m >= 10 ? 'm-ok' : m > 0 ? 'm-low' : 'm-loss'; }

  formatMoney(v: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(v || 0);
  }
}
