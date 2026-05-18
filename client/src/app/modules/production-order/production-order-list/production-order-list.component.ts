import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ProductionOrderService } from '../../../services/production-order.service';
import { ProductService } from '../../../services/product.service';
import { BomService } from '../../../services/bom.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  PRODUCTION_ORDER_STATUSES,
  ProductionOrderDto,
  ProductionOrderListItemDto
} from '../../../models/production-order.models';
import { ProductListItemDto } from '../../../models/product.models';
import { BomListItemDto } from '../../../models/bom.models';
import { WarehouseDto } from '../../../models/master-data.models';

interface BomOption extends BomListItemDto {
  displayLabel: string;     // "BTX/BOM/2026/00001 — v3 (Active)"
}

@Component({
  selector: 'app-production-order-list',
  standalone: false,
  templateUrl: './production-order-list.component.html',
  styleUrl: './production-order-list.component.scss'
})
export class ProductionOrderListComponent implements OnInit {

  pos: ProductionOrderListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterProductId: number | null = null;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = PRODUCTION_ORDER_STATUSES;
  products: ProductListItemDto[] = [];
  bomsForSelectedProduct: BomOption[] = [];
  warehouses: WarehouseDto[] = [];

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  loadedDetail: ProductionOrderDto | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingPo: ProductionOrderListItemDto | null = null;
  deleting = false;
  deleteError = '';

  // Row action (start / complete / cancel) in-flight id
  rowActionId: number | null = null;

  constructor(
    private poService: ProductionOrderService,
    private productService: ProductService,
    private bomService: BomService,
    private warehouseService: WarehouseService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadDropdowns();
    this.load();
  }

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private buildForm(): void {
    this.form = this.fb.group({
      productId: [null as number | null, Validators.required],
      bomId: [null as number | null, Validators.required],
      quantity: [1, [Validators.required, Validators.min(0.0001)]],
      issueWarehouseId: [null as number | null, Validators.required],
      receiveWarehouseId: [null as number | null, Validators.required],
      plannedStartDate: [this.todayIso() as string | null],
      plannedEndDate: [null as string | null],
      notes: ['', Validators.maxLength(2000)]
    });
  }

  // ─── Data loading ────────────────────────────────────────────────────────

  private loadDropdowns(): void {
    this.productService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.products = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
    this.warehouseService.getAll(undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success) this.warehouses = res.data ?? [];
          this.cdr.detectChanges();
        });
      }
    });
  }

  private loadBomsForProduct(productId: number, preferredBomId: number | null = null): void {
    this.bomService.getAll({ page: 1, pageSize: 100, search: '' }, productId).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.bomsForSelectedProduct = res.data.items.map(b => ({
              ...b,
              displayLabel: `${b.code} — v${b.version}${b.isActive ? ' (Active)' : ''} [${b.status}]`
            }));
            // Pick preferred (when editing) or the Active BOM if any
            if (preferredBomId) {
              this.form.get('bomId')?.setValue(preferredBomId);
            } else {
              const active = this.bomsForSelectedProduct.find(b => b.isActive);
              if (active) this.form.get('bomId')?.setValue(active.id);
            }
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  onProductChange(event: any): void {
    const productId = event?.value;
    this.form.get('bomId')?.setValue(null);
    this.bomsForSelectedProduct = [];
    if (productId) this.loadBomsForProduct(productId);
  }

  load(): void {
    this.loading = true;
    this.poService.getAll(
      this.parameters,
      this.filterProductId ?? undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.pos = res.data.items;
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

  // ─── Dialog ──────────────────────────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.loadedDetail = null;
    this.bomsForSelectedProduct = [];
    this.form.enable();
    this.form.reset({
      productId: this.filterProductId ?? null,
      bomId: null,
      quantity: 1,
      issueWarehouseId: this.warehouses[0]?.id ?? null,
      receiveWarehouseId: this.warehouses[0]?.id ?? null,
      plannedStartDate: this.todayIso(),
      plannedEndDate: null,
      notes: ''
    });
    this.dialogVisible = true;
    if (this.form.get('productId')?.value) {
      this.loadBomsForProduct(this.form.get('productId')!.value);
    }
  }

  openEdit(po: ProductionOrderListItemDto): void {
    this.editingId = po.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.loadedDetail = null;
    this.bomsForSelectedProduct = [];
    this.form.enable();
    this.dialogVisible = true;

    this.poService.getById(po.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const p = res.data;
            this.loadedDetail = p;
            this.dialogMode = p.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              productId: p.productId,
              bomId: p.bomId,
              quantity: p.quantity,
              issueWarehouseId: p.issueWarehouseId,
              receiveWarehouseId: p.receiveWarehouseId,
              plannedStartDate: p.plannedStartDate ?? null,
              plannedEndDate: p.plannedEndDate ?? null,
              notes: p.notes ?? ''
            });
            this.loadBomsForProduct(p.productId, p.bomId);
            if (this.dialogMode === 'view') this.form.disable();
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving || this.dialogMode === 'view') return;

    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();
    const payload = {
      productId: v.productId,
      bomId: v.bomId,
      quantity: Number(v.quantity) || 0,
      issueWarehouseId: v.issueWarehouseId,
      receiveWarehouseId: v.receiveWarehouseId,
      plannedStartDate: v.plannedStartDate || null,
      plannedEndDate: v.plannedEndDate || null,
      notes: (v.notes as string)?.trim() || null
    };

    if (this.dialogMode === 'create') {
      this.poService.create(payload).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.poService.update(this.editingId, payload).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    }
  }

  private handleSave(res: { success: boolean; message?: string | null }): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      if (res.success) {
        this.dialogVisible = false;
        this.load();
      } else {
        this.dialogError = res.message || 'Save failed.';
      }
      this.cdr.detectChanges();
    });
  }

  private handleError(err: any): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      this.dialogError = err?.error?.message || 'Save failed.';
      this.cdr.detectChanges();
    });
  }

  // ─── Row actions ─────────────────────────────────────────────────────────

  start(po: ProductionOrderListItemDto): void {
    this.runRowAction(po, this.poService.start.bind(this.poService));
  }

  complete(po: ProductionOrderListItemDto): void {
    this.runRowAction(po, this.poService.complete.bind(this.poService));
  }

  cancel(po: ProductionOrderListItemDto): void {
    this.runRowAction(po, this.poService.cancel.bind(this.poService));
  }

  private runRowAction(po: ProductionOrderListItemDto, fn: (id: number) => any): void {
    if (this.rowActionId) return;
    this.rowActionId = po.id;
    this.actionError = '';
    this.cdr.detectChanges();
    fn(po.id).subscribe({
      next: (res: { success: boolean; message?: string | null }) => this.handleRowAction(res),
      error: (err: any) => this.handleRowActionError(err)
    });
  }

  private handleRowAction(res: { success: boolean; message?: string | null }): void {
    this.zone.run(() => {
      this.rowActionId = null;
      if (res.success) {
        this.actionError = '';
        this.load();
      } else {
        this.actionError = res.message || 'Action failed.';
      }
      this.cdr.detectChanges();
    });
  }

  private handleRowActionError(err: any): void {
    this.zone.run(() => {
      this.rowActionId = null;
      this.actionError = err?.error?.message || 'Action failed.';
      this.cdr.detectChanges();
    });
  }

  canCancel(po: ProductionOrderListItemDto): boolean {
    return po.status === 'Draft' || po.status === 'InProgress';
  }

  canDelete(po: ProductionOrderListItemDto): boolean {
    return po.status === 'Draft' || po.status === 'Cancelled';
  }

  // ─── Delete ──────────────────────────────────────────────────────────────

  confirmDelete(po: ProductionOrderListItemDto): void {
    this.deletingPo = po;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingPo || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.poService.delete(this.deletingPo.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingPo = null;
            this.load();
          } else {
            this.deleteError = res.message || 'Delete failed.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.deleting = false;
          this.deleteError = err?.error?.message || 'Delete failed.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  formatDate(d: string | null): string {
    return d ? d : '—';
  }
}
