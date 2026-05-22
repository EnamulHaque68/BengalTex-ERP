import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ExpenseService } from '../../../services/expense.service';
import { ExpenseSummaryDto } from '../../../models/expense.models';

@Component({
  selector: 'app-expense-summary',
  standalone: false,
  templateUrl: './expense-summary.component.html',
  styleUrl: './expense-summary.component.scss'
})
export class ExpenseSummaryComponent implements OnInit {
  fromDate: string;
  toDate: string;
  data: ExpenseSummaryDto | null = null;
  loading = false;

  constructor(private svc: ExpenseService, private zone: NgZone, private cdr: ChangeDetectorRef) {
    const now = new Date();
    this.fromDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
    this.toDate = now.toISOString().slice(0, 10);
  }

  ngOnInit(): void { this.run(); }

  run(): void {
    this.loading = true;
    this.svc.summary(this.fromDate, this.toDate).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success) this.data = res.data ?? null; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  pct(amount: number): number {
    if (!this.data || this.data.totalAmount === 0) return 0;
    return Math.round((amount / this.data.totalAmount) * 100);
  }
}
