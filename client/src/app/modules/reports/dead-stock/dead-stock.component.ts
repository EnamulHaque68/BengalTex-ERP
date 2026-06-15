import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { DeadStockReportDto } from '../../../models/reports.models';
import { WarehouseDto } from '../../../models/master-data.models';

@Component({
  selector: 'app-dead-stock',
  standalone: false,
  templateUrl: './dead-stock.component.html',
  styleUrl: './dead-stock.component.scss'
})
export class DeadStockComponent implements OnInit {

  report: DeadStockReportDto | null = null;
  loading = false;
  error = '';

  daysThreshold = 90;
  filterItemType: string | null = null;
  filterWarehouseId: number | null = null;

  thresholdOptions = [
    { label: '30+ days', value: 30 },
    { label: '60+ days', value: 60 },
    { label: '90+ days', value: 90 },
    { label: '180+ days', value: 180 },
    { label: '365+ days', value: 365 }
  ];
  itemTypes = [
    { label: 'Raw Materials only', value: 'RawMaterial' },
    { label: 'Products only',      value: 'Product' }
  ];
  warehouses: WarehouseDto[] = [];

  constructor(
    private reportsService: ReportsService,
    private warehouseService: WarehouseService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadWarehouses();
    this.load();
  }

  private loadWarehouses(): void {
    this.warehouseService.getAll(undefined, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.warehouses = res.data;
        this.cdr.detectChanges();
      })
    });
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();
    this.reportsService.getDeadStock(
      this.daysThreshold,
      this.filterItemType ?? undefined,
      this.filterWarehouseId ?? undefined
    ).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.report = res.data;
        else this.error = res.message || 'Failed to load report.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loading = false;
        this.error = err?.error?.message || 'Failed to load report.';
        this.cdr.detectChanges();
      })
    });
  }

  formatNumber(n: number): string {
    return new Intl.NumberFormat('en-BD', { maximumFractionDigits: 4 }).format(n || 0);
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency', currency: 'BDT', maximumFractionDigits: 2
    }).format(amount || 0);
  }

  agingClass(days: number): string {
    if (days >= 365) return 'sev-critical';
    if (days >= 180) return 'sev-high';
    if (days >= 90) return 'sev-mid';
    return 'sev-low';
  }

  print(): void { window.print(); }
}
