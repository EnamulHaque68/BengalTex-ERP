import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService, ProductionCostSheetDto, WipReportDto } from '../../../services/accounting.service';

/** Phase A4 — production costing: fully-loaded cost sheet + WIP valuation. */
@Component({
  selector: 'app-production-costing',
  standalone: false,
  templateUrl: './production-costing.component.html',
  styleUrl: './production-costing.component.scss'
})
export class ProductionCostingComponent implements OnInit {
  tab: 'cost-sheet' | 'wip' = 'cost-sheet';
  fromDate = '';
  toDate = '';
  loading = false;
  sheet: ProductionCostSheetDto | null = null;
  wip: WipReportDto | null = null;

  constructor(private svc: AccountingService, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    const now = new Date();
    this.fromDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
    this.toDate = now.toISOString().slice(0, 10);
    this.load();
  }

  setTab(t: 'cost-sheet' | 'wip'): void { this.tab = t; this.load(); }

  load(): void {
    this.loading = true; this.sheet = null; this.wip = null;
    if (this.tab === 'cost-sheet') {
      this.svc.productionCostSheet(this.fromDate, this.toDate).subscribe({
        next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.sheet = res.data; this.cdr.detectChanges(); }),
        error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
      });
    } else {
      this.svc.wipValuation(this.toDate).subscribe({
        next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.wip = res.data; this.cdr.detectChanges(); }),
        error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
      });
    }
  }

  formatMoney(v: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(v || 0);
  }
}
