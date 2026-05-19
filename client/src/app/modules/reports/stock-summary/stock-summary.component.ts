import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { StockSummaryReportDto } from '../../../models/reports.models';
import { WarehouseDto } from '../../../models/master-data.models';

@Component({
  selector: 'app-stock-summary',
  standalone: false,
  templateUrl: './stock-summary.component.html',
  styleUrl: './stock-summary.component.scss'
})
export class StockSummaryComponent implements OnInit {

  report: StockSummaryReportDto | null = null;
  loading = false;
  error = '';

  filterItemType: string | null = null;
  filterWarehouseId: number | null = null;

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
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.warehouses = res.data;
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();
    this.reportsService.getStockSummary(
      this.filterItemType ?? undefined,
      this.filterWarehouseId ?? undefined
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

  formatNumber(n: number): string {
    return new Intl.NumberFormat('en-BD', { maximumFractionDigits: 4 }).format(n || 0);
  }

  print(): void {
    window.print();
  }
}
