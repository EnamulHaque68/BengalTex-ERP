import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ProductionOrderService } from '../../../services/production-order.service';
import {
  ProductionOrderDto,
  ProductionOrderListItemDto,
  ProductionStageDto
} from '../../../models/production-order.models';

/**
 * Mobile-first shop-floor stage control. An operator picks (or scans into the search box) an
 * in-progress production order and drives its routing stages — Start / Complete (qty + rejected) /
 * Skip — with large touch targets. Reuses the existing /production-orders/stages/* endpoints and
 * the sequential-stage gating enforced server-side. No new backend.
 */
@Component({
  selector: 'app-shop-floor',
  standalone: false,
  templateUrl: './shop-floor.component.html',
  styleUrl: './shop-floor.component.scss'
})
export class ShopFloorComponent implements OnInit {
  orders: ProductionOrderListItemDto[] = [];
  filtered: ProductionOrderListItemDto[] = [];
  search = '';
  loadingList = false;

  order: ProductionOrderDto | null = null;
  loadingOrder = false;
  error = '';
  actionStageId: number | null = null;

  // Inline "complete stage" panel
  completingStageId: number | null = null;
  completeQty = 0;
  rejectQty = 0;

  constructor(
    private svc: ProductionOrderService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.loadingList = true;
    this.svc.getAll({ page: 1, pageSize: 200, search: '' }, undefined, 'InProgress').subscribe({
      next: (res) => this.zone.run(() => {
        this.loadingList = false;
        if (res.success && res.data) {
          this.orders = res.data.items;
          this.applyFilter();
        }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loadingList = false; this.cdr.detectChanges(); })
    });
  }

  applyFilter(): void {
    const q = this.search.trim().toLowerCase();
    this.filtered = !q
      ? this.orders
      : this.orders.filter(o =>
          o.code.toLowerCase().includes(q) || o.productName.toLowerCase().includes(q));
  }

  onSearch(value: string): void { this.search = value; this.applyFilter(); }

  selectOrder(o: ProductionOrderListItemDto): void {
    this.loadOrder(o.id);
  }

  private loadOrder(id: number): void {
    this.loadingOrder = true;
    this.error = '';
    this.completingStageId = null;
    this.cdr.detectChanges();
    this.svc.getById(id).subscribe({
      next: (res) => this.zone.run(() => {
        this.loadingOrder = false;
        if (res.success && res.data) this.order = res.data;
        else this.error = res.message || 'Could not load order.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loadingOrder = false;
        this.error = err?.error?.message || 'Could not load order.';
        this.cdr.detectChanges();
      })
    });
  }

  backToList(): void { this.order = null; this.error = ''; this.loadOrders(); }

  get sortedStages(): ProductionStageDto[] {
    return (this.order?.stages ?? []).slice().sort((a, b) => a.sequence - b.sequence);
  }

  stageRemaining(s: ProductionStageDto): number {
    return Math.max(0, (s.plannedQuantity || 0) - (s.completedQuantity || 0));
  }

  // ── Stage actions ──
  startStage(s: ProductionStageDto): void {
    if (this.actionStageId) return;
    this.runStageAction(s.id, this.svc.startStage(s.id));
  }

  skipStage(s: ProductionStageDto): void {
    if (this.actionStageId) return;
    this.runStageAction(s.id, this.svc.skipStage(s.id));
  }

  openComplete(s: ProductionStageDto): void {
    this.completingStageId = s.id;
    this.completeQty = this.stageRemaining(s) || s.plannedQuantity || 0;
    this.rejectQty = 0;
  }

  cancelComplete(): void { this.completingStageId = null; }

  confirmComplete(s: ProductionStageDto): void {
    if (this.actionStageId) return;
    this.runStageAction(s.id, this.svc.completeStage(s.id, {
      completedQuantity: Number(this.completeQty) || 0,
      rejectedQuantity: Number(this.rejectQty) || 0,
      notes: null
    }));
  }

  private runStageAction(stageId: number, obs: ReturnType<ProductionOrderService['startStage']>): void {
    this.actionStageId = stageId;
    this.error = '';
    this.cdr.detectChanges();
    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.actionStageId = null;
        this.completingStageId = null;
        if (res.success && res.data) this.order = res.data;
        else this.error = res.message || 'Action failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.actionStageId = null;
        this.error = err?.error?.message || 'Action failed.';
        this.cdr.detectChanges();
      })
    });
  }

  stageClass(status: string): string {
    switch (status) {
      case 'Pending': return 'st-pending';
      case 'InProgress': return 'st-progress';
      case 'Completed': return 'st-done';
      case 'Skipped': return 'st-skip';
      default: return '';
    }
  }
}
