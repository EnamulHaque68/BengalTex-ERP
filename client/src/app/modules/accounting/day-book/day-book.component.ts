import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService } from '../../../services/accounting.service';
import { DayBookDto } from '../../../models/accounting.models';

@Component({
  selector: 'app-day-book',
  standalone: false,
  templateUrl: './day-book.component.html',
  styleUrl: './day-book.component.scss'
})
export class DayBookComponent implements OnInit {
  fromDate: string;
  toDate: string;
  data: DayBookDto | null = null;
  loading = false;
  error = '';
  expanded = new Set<number>();

  constructor(private svc: AccountingService, private zone: NgZone, private cdr: ChangeDetectorRef) {
    const now = new Date();
    this.fromDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
    this.toDate = now.toISOString().slice(0, 10);
  }

  ngOnInit(): void { this.run(); }

  run(): void {
    this.error = '';
    this.loading = true;
    this.expanded.clear();
    this.svc.dayBook(this.fromDate, this.toDate).subscribe({
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

  toggle(id: number): void {
    if (this.expanded.has(id)) this.expanded.delete(id);
    else this.expanded.add(id);
  }

  expandAll(): void {
    if (!this.data) return;
    this.data.entries.forEach(e => this.expanded.add(e.journalEntryId));
  }

  collapseAll(): void { this.expanded.clear(); }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }
}
