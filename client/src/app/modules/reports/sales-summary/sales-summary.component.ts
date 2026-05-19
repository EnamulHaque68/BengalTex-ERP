import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import { CustomerService } from '../../../services/customer.service';
import { SalesSummaryReportDto } from '../../../models/reports.models';
import { CustomerListItemDto } from '../../../models/customer.models';

@Component({
  selector: 'app-sales-summary',
  standalone: false,
  templateUrl: './sales-summary.component.html',
  styleUrl: './sales-summary.component.scss'
})
export class SalesSummaryComponent implements OnInit {

  report: SalesSummaryReportDto | null = null;
  loading = false;
  error = '';

  fromDate: string = '';
  toDate: string = '';
  filterCustomerId: number | null = null;
  customers: CustomerListItemDto[] = [];

  constructor(
    private reportsService: ReportsService,
    private customerService: CustomerService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const today = new Date();
    const thirtyAgo = new Date(today);
    thirtyAgo.setDate(today.getDate() - 30);
    this.toDate = today.toISOString().slice(0, 10);
    this.fromDate = thirtyAgo.toISOString().slice(0, 10);
    this.loadCustomers();
    this.load();
  }

  private loadCustomers(): void {
    this.customerService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.customers = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();

    this.reportsService.getSalesSummary(
      this.fromDate || undefined,
      this.toDate || undefined,
      this.filterCustomerId ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.report = res.data;
          } else {
            this.error = res.message || 'Failed to load report.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.loading = false;
          this.error = err?.error?.message || 'Failed to load report.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency', currency: 'BDT', maximumFractionDigits: 2
    }).format(amount || 0);
  }

  print(): void {
    window.print();
  }
}
