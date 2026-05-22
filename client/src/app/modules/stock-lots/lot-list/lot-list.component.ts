import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { StockLotService } from '../../../services/stock-lot.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  LOT_ITEM_TYPES, LOT_STATUSES,
  StockLotDto, StockLotMovementDto
} from '../../../models/stock-lot.models';

@Component({
  selector: 'app-lot-list',
  standalone: false,
  templateUrl: './lot-list.component.html',
  styleUrl: './lot-list.component.scss'
})
export class LotListComponent implements OnInit {

  lots: StockLotDto[] = [];
  loading = false;
  totalCount = 0;

  filterItemType: string | null = null;
  filterStatus: string | null = null;
  expiringSoon = false;     // expiry within 30 days (or already expired)
  activeOnly = false;       // currentQuantity > 0

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly itemTypes = LOT_ITEM_TYPES;
  readonly statuses = LOT_STATUSES;

  // Detail dialog
  viewDialogVisible = false;
  viewing: StockLotDto | null = null;
  movements: StockLotMovementDto[] = [];
  detailLoading = false;

  constructor(
    private lotService: StockLotService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.lotService.getAll(
      this.parameters,
      this.filterItemType ?? undefined,
      undefined,
      undefined,
      this.filterStatus ?? undefined,
      this.expiringSoon ? 30 : undefined,
      this.activeOnly
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.lots = res.data.items;
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

  openView(lot: StockLotDto): void {
    this.viewing = lot;
    this.movements = [];
    this.viewDialogVisible = true;
    this.detailLoading = true;
    this.lotService.getById(lot.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.detailLoading = false;
          if (res.success && res.data) {
            this.viewing = res.data.lot;
            this.movements = res.data.movements;
          }
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => { this.detailLoading = false; this.cdr.detectChanges(); });
      }
    });
  }

  statusClass(lot: StockLotDto): string {
    if (lot.isExpired) return 'expired';
    switch (lot.status) {
      case 'Active':      return 'active';
      case 'Depleted':    return 'depleted';
      case 'Quarantined': return 'quarantined';
      case 'Expired':     return 'expired';
      default:            return '';
    }
  }

  statusLabel(lot: StockLotDto): string {
    // Surface expiry even when the stored status is still Active
    if (lot.isExpired && lot.status === 'Active') return 'Expired';
    return lot.status;
  }
}
