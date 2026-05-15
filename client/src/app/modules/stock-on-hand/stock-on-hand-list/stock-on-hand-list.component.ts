import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { StockService } from '../../../services/stock.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { StockOnHandDto } from '../../../models/inventory.models';
import { WarehouseDto } from '../../../models/master-data.models';
import { RawMaterialListItemDto } from '../../../models/raw-material.models';

@Component({
  selector: 'app-stock-on-hand-list',
  standalone: false,
  templateUrl: './stock-on-hand-list.component.html',
  styleUrl: './stock-on-hand-list.component.scss'
})
export class StockOnHandListComponent implements OnInit {

  rows: StockOnHandDto[] = [];
  loading = false;
  totalCount = 0;
  filterWarehouseId: number | null = null;
  filterRawMaterialId: number | null = null;
  belowMinimumOnly = false;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  warehouses: WarehouseDto[] = [];
  rawMaterials: RawMaterialListItemDto[] = [];

  constructor(
    private stockService: StockService,
    private warehouseService: WarehouseService,
    private rawMaterialService: RawMaterialService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadDropdowns();
    this.load();
  }

  private loadDropdowns(): void {
    this.warehouseService.getAll(undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success) this.warehouses = res.data ?? [];
          this.cdr.detectChanges();
        });
      }
    });
    this.rawMaterialService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.rawMaterials = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.stockService.getOnHand(
      this.parameters,
      this.filterWarehouseId ?? undefined,
      this.filterRawMaterialId ?? undefined,
      this.belowMinimumOnly
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.rows = res.data.items;
            this.totalCount = res.data.totalCount;
          }
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); });
      }
    });
  }

  onSearchChange(value: string): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.parameters.search = value;
      this.parameters.page = 1;
      this.load();
    }, 400);
  }

  onPageChange(event: any): void {
    this.parameters.page = Math.floor(event.first / event.rows) + 1;
    this.parameters.pageSize = event.rows;
    this.load();
  }
}
