import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { ReportsService } from '../../../services/reports.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { ProductService } from '../../../services/product.service';
import { StockLedgerReportDto } from '../../../models/reports.models';
import { WarehouseDto } from '../../../models/master-data.models';

interface ItemOption { id: number; label: string; }

@Component({
  selector: 'app-stock-ledger',
  standalone: false,
  templateUrl: './stock-ledger.component.html',
  styleUrl: './stock-ledger.component.scss'
})
export class StockLedgerComponent implements OnInit {

  report: StockLedgerReportDto | null = null;
  loading = false;
  error = '';

  // Filters
  filterItemType: 'RawMaterial' | 'Product' = 'RawMaterial';
  filterItemId: number | null = null;
  filterWarehouseId: number | null = null;
  fromDate: string | null = null;
  toDate: string | null = null;

  itemTypes = [
    { label: 'Raw Material', value: 'RawMaterial' },
    { label: 'Product', value: 'Product' }
  ];
  items: ItemOption[] = [];
  itemsLoading = false;
  warehouses: WarehouseDto[] = [];

  constructor(
    private reportsService: ReportsService,
    private warehouseService: WarehouseService,
    private rmService: RawMaterialService,
    private productService: ProductService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadWarehouses();
    this.loadItems();
  }

  private loadWarehouses(): void {
    this.warehouseService.getAll(undefined, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.warehouses = res.data;
        this.cdr.detectChanges();
      })
    });
  }

  /** Reload the item picker for the selected item type; clears any current selection. */
  onItemTypeChange(): void {
    this.filterItemId = null;
    this.items = [];
    this.report = null;
    this.loadItems();
  }

  private loadItems(): void {
    this.itemsLoading = true;
    this.cdr.detectChanges();
    const params = { page: 1, pageSize: 1000, search: '' };
    const obs: Observable<any> = this.filterItemType === 'RawMaterial'
      ? this.rmService.getAll(params, undefined, false)
      : this.productService.getAll(params, undefined, false);
    obs.subscribe({
      next: (res: any) => this.zone.run(() => {
        this.itemsLoading = false;
        if (res.success && res.data) {
          this.items = res.data.items.map((i: any) => ({ id: i.id, label: `${i.code} — ${i.name}` }));
        }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.itemsLoading = false; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    if (!this.filterItemId) { this.error = 'Select an item first.'; this.cdr.detectChanges(); return; }
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();
    this.reportsService.getStockLedger(
      this.filterItemType,
      this.filterItemId,
      this.filterWarehouseId ?? undefined,
      this.fromDate ?? undefined,
      this.toDate ?? undefined
    ).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.report = res.data;
        else this.error = res.message || 'Failed to load ledger.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loading = false;
        this.error = err?.error?.message || 'Failed to load ledger.';
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

  print(): void { window.print(); }
}
