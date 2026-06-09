import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import { CustomerService } from '../../../services/customer.service';
import { BuyerOrderBookReportDto } from '../../../models/reports.models';
import { CustomerListItemDto } from '../../../models/customer.models';

@Component({
  selector: 'app-buyer-order-book',
  standalone: false,
  templateUrl: './buyer-order-book.component.html',
  styleUrl: './buyer-order-book.component.scss'
})
export class BuyerOrderBookComponent implements OnInit {
  report: BuyerOrderBookReportDto | null = null;
  loading = false;
  error = '';

  filterCustomerId: number | null = null;
  customers: CustomerListItemDto[] = [];
  expanded = new Set<number>();

  constructor(
    private svc: ReportsService,
    private customerService: CustomerService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.customerService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.customers = res.data.items;
        this.cdr.detectChanges();
      })
    });
    this.run();
  }

  run(): void {
    this.loading = true;
    this.error = '';
    this.expanded.clear();
    this.cdr.detectChanges();
    this.svc.getBuyerOrderBook(this.filterCustomerId ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.report = res.data;
          // auto-expand when filtering a single buyer
          if (this.filterCustomerId && res.data.rows.length === 1) {
            this.expanded.add(res.data.rows[0].customerId);
          }
        } else {
          this.report = null;
          this.error = res.message || 'Failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => {
        this.loading = false;
        this.error = e?.error?.message || 'Failed.';
        this.cdr.detectChanges();
      })
    });
  }

  toggle(customerId: number): void {
    if (this.expanded.has(customerId)) this.expanded.delete(customerId);
    else this.expanded.add(customerId);
  }

  expandAll(): void {
    if (!this.report) return;
    this.report.rows.forEach(r => this.expanded.add(r.customerId));
  }
  collapseAll(): void { this.expanded.clear(); }

  formatCurrency(amount: number, code = 'BDT'): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: code, maximumFractionDigits: 2 }).format(amount || 0);
  }

  formatNumber(n: number): string {
    return new Intl.NumberFormat('en-BD', { maximumFractionDigits: 2 }).format(n || 0);
  }

  creditUsagePercent(row: { creditLimit: number | null; outstandingInvoiceBdt: number }): number {
    if (!row.creditLimit || row.creditLimit <= 0) return 0;
    return Math.round((row.outstandingInvoiceBdt / row.creditLimit) * 100);
  }

  creditClass(pct: number): string {
    if (pct >= 90) return 'bad';
    if (pct >= 70) return 'warn';
    return 'good';
  }
}
