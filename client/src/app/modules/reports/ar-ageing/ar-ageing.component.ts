import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import { CustomerService } from '../../../services/customer.service';
import { ArAgeingReportDto } from '../../../models/reports.models';
import { CustomerListItemDto } from '../../../models/customer.models';

@Component({
  selector: 'app-ar-ageing',
  standalone: false,
  templateUrl: './ar-ageing.component.html',
  styleUrl: './ar-ageing.component.scss'
})
export class ArAgeingComponent implements OnInit {

  report: ArAgeingReportDto | null = null;
  loading = false;
  error = '';

  asOfDate: string = '';
  filterCustomerId: number | null = null;
  customers: CustomerListItemDto[] = [];

  expandedRows: { [key: number]: boolean } = {};

  constructor(
    private reportsService: ReportsService,
    private customerService: CustomerService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.asOfDate = this.todayIso();
    this.loadCustomers();
    this.load();
  }

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
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
    this.expandedRows = {};
    this.cdr.detectChanges();

    this.reportsService.getArAgeing(
      this.asOfDate || undefined,
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

  toggleExpand(customerId: number): void {
    this.expandedRows[customerId] = !this.expandedRows[customerId];
  }

  isExpanded(customerId: number): boolean {
    return !!this.expandedRows[customerId];
  }

  bucketClass(bucket: string): string {
    switch (bucket) {
      case 'Current': return 'current';
      case '1-30':    return 'b1-30';
      case '31-60':   return 'b31-60';
      case '61-90':   return 'b61-90';
      case '90+':     return 'b90plus';
      default:        return '';
    }
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
