import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService } from '../../../services/accounting.service';
import { CashBookDto } from '../../../models/accounting.models';

@Component({
  selector: 'app-cash-book',
  standalone: false,
  templateUrl: './cash-book.component.html',
  styleUrl: './cash-book.component.scss'
})
export class CashBookComponent implements OnInit {
  fromDate: string;
  toDate: string;
  data: CashBookDto | null = null;
  loading = false;
  error = '';

  constructor(private svc: AccountingService, private zone: NgZone, private cdr: ChangeDetectorRef) {
    const now = new Date();
    this.fromDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
    this.toDate = now.toISOString().slice(0, 10);
  }

  ngOnInit(): void { this.run(); }

  run(): void {
    this.error = '';
    this.loading = true;
    this.svc.cashBook(this.fromDate, this.toDate).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success) this.data = res.data ?? null;
        else { this.data = null; this.error = res.message || 'Failed.'; }
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => {
        this.loading = false;
        this.error = e?.error?.message || 'Failed.';
        this.cdr.detectChanges();
      })
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }
}
