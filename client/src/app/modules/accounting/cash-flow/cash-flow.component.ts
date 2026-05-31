import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService } from '../../../services/accounting.service';
import { CashFlowStatementDto } from '../../../models/accounting.models';

@Component({
  selector: 'app-cash-flow',
  standalone: false,
  templateUrl: './cash-flow.component.html',
  styleUrl: './cash-flow.component.scss'
})
export class CashFlowComponent implements OnInit {
  fromDate: string;
  toDate: string;
  data: CashFlowStatementDto | null = null;
  loading = false;
  expanded: Record<string, boolean> = { Operating: true, Investing: false, Financing: false };

  constructor(private svc: AccountingService, private zone: NgZone, private cdr: ChangeDetectorRef) {
    const now = new Date();
    this.fromDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
    this.toDate = now.toISOString().slice(0, 10);
  }

  ngOnInit(): void { this.run(); }

  run(): void {
    this.loading = true;
    this.svc.cashFlow(this.fromDate, this.toDate).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success) this.data = res.data ?? null; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  toggle(section: string): void { this.expanded[section] = !this.expanded[section]; }

  sectionColor(name: string): string {
    return name === 'Operating' ? '#2563eb' : name === 'Investing' ? '#7c3aed' : '#059669';
  }

  format(n: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(n || 0);
  }
}
