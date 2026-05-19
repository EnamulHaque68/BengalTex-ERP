import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import { SupplierService } from '../../../services/supplier.service';
import { ApAgeingReportDto } from '../../../models/reports.models';
import { SupplierListItemDto } from '../../../models/supplier.models';

@Component({
  selector: 'app-ap-ageing',
  standalone: false,
  templateUrl: './ap-ageing.component.html',
  styleUrl: './ap-ageing.component.scss'
})
export class ApAgeingComponent implements OnInit {

  report: ApAgeingReportDto | null = null;
  loading = false;
  error = '';

  asOfDate: string = '';
  filterSupplierId: number | null = null;
  suppliers: SupplierListItemDto[] = [];

  expandedRows: { [key: number]: boolean } = {};

  constructor(
    private reportsService: ReportsService,
    private supplierService: SupplierService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.asOfDate = this.todayIso();
    this.loadSuppliers();
    this.load();
  }

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private loadSuppliers(): void {
    this.supplierService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.suppliers = res.data.items;
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

    this.reportsService.getApAgeing(
      this.asOfDate || undefined,
      this.filterSupplierId ?? undefined
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

  toggleExpand(supplierId: number): void {
    this.expandedRows[supplierId] = !this.expandedRows[supplierId];
  }

  isExpanded(supplierId: number): boolean {
    return !!this.expandedRows[supplierId];
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
