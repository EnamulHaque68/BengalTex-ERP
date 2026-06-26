import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ProductionOrderService } from '../../../services/production-order.service';
import { ProductService } from '../../../services/product.service';
import { BomService } from '../../../services/bom.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { EmployeeService } from '../../../services/employee.service';
import { SalesOrderService } from '../../../services/sales-order.service';
import { SalesOrderDto, SalesOrderLineDto, SalesOrderListItemDto } from '../../../models/sales-order.models';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  COMMON_STAGE_NAMES,
  PRODUCTION_ORDER_STATUSES,
  ProductionOrderDto,
  ProductionOrderListItemDto,
  ProductionStageDto
} from '../../../models/production-order.models';
import { ProductListItemDto } from '../../../models/product.models';
import { BomListItemDto } from '../../../models/bom.models';
import { WarehouseDto } from '../../../models/master-data.models';
import { EmployeeListItemDto } from '../../../models/employee.models';

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
  readonly commonStageNames = COMMON_STAGE_NAMES.map(n => ({ label: n, value: n }));
  products: ProductListItemDto[] = [];
  bomsForSelectedProduct: BomOption[] = [];
  warehouses: WarehouseDto[] = [];
  employees: EmployeeListItemDto[] = [];

  // Phase 1 — optional source Sales Order (drives the fulfilling line + remaining quantity)
  produceableSos: SalesOrderListItemDto[] = [];
  selectedSo: SalesOrderDto | null = null;
  selectedSoLine: SalesOrderLineDto | null = null;

  // Stage-tracking dialog (shop-floor progress on an order's routing)
  stagesDialogVisible = false;
  trackingOrder: ProductionOrderDto | null = null;
  stageActionId: number | null = null;
  stagesError = '';
  completingStageId: number | null = null;
  completeQty = 0;
  rejectQty = 0;

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
    private employeeService: EmployeeService,
    private soService: SalesOrderService,
    private router: Router,
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
      salesOrderId: [null as number | null],          // Phase 1 — optional source SO
      salesOrderLineId: [null as number | null],
      productId: [null as number | null, Validators.required],
      bomId: [null as number | null, Validators.required],
      quantity: [1, [Validators.required, Validators.min(0.0001)]],
      issueWarehouseId: [null as number | null, Validators.required],
      receiveWarehouseId: [null as number | null, Validators.required],
      plannedStartDate: [this.todayIso() as string | null],
      plannedEndDate: [null as string | null],
      notes: ['', Validators.maxLength(2000)],
      stages: this.fb.array([])
    });
  }

  get stages(): FormArray {
    return this.form.get('stages') as FormArray;
  }

  addStage(stageName = ''): void {
    this.stages.push(this.fb.group({
      stageName: [stageName, [Validators.required, Validators.maxLength(100)]],
      plannedQuantity: [null as number | null],
      productionLine: ['', Validators.maxLength(100)],
      operatorEmployeeId: [null as number | null],
      notes: ['', Validators.maxLength(1000)]
    }));
  }

  removeStage(i: number): void {
    this.stages.removeAt(i);
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
    this.employeeService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.employees = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
    // Phase 1 — sales orders that can drive production (confirmed onward, not draft/cancelled)
    this.soService.getAll({ page: 1, pageSize: 500, search: '' }).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.produceableSos = res.data.items.filter(s =>
              s.status !== 'Draft' && s.status !== 'Cancelled' && s.status !== 'PendingApproval');
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  // ─── Phase 1: source Sales Order → fulfilling line → remaining qty ─────────

  onSourceSoChange(event: any): void {
    const soId = event?.value;
    this.selectedSo = null;
    this.selectedSoLine = null;
    this.form.get('salesOrderLineId')?.setValue(null);
    if (!soId) return;
    this.soService.getById(soId).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.selectedSo = res.data;
        this.cdr.detectChanges();
      })
    });
  }

  onSourceLineChange(event: any): void {
    const lineId = event?.value;
    const line = this.selectedSo?.lines.find(l => l.id === lineId) ?? null;
    this.selectedSoLine = line;
    if (line) {
      this.form.get('productId')?.setValue(line.productId);
      const remaining = this.lineRemaining(line);
      this.form.get('quantity')?.setValue(remaining > 0 ? remaining : line.quantity);
      this.loadBomsForProduct(line.productId);
    }
  }

  lineRemaining(line: SalesOrderLineDto): number {
    return (line.quantity ?? 0) - (line.allocatedQuantity ?? 0);
  }

  get sourceLineOptions(): { label: string; value: number }[] {
    if (!this.selectedSo) return [];
    return this.selectedSo.lines.map(l => ({
      label: `${l.productName} — ordered ${l.quantity}, remaining ${this.lineRemaining(l)}`,
      value: l.id
    }));
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
    this.selectedSo = null;
    this.selectedSoLine = null;
    this.form.enable();
    this.stages.clear();
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
              salesOrderId: p.salesOrderId ?? null,
              salesOrderLineId: p.salesOrderLineId ?? null,
              productId: p.productId,
              bomId: p.bomId,
              quantity: p.quantity,
              issueWarehouseId: p.issueWarehouseId,
              receiveWarehouseId: p.receiveWarehouseId,
              plannedStartDate: p.plannedStartDate ?? null,
              plannedEndDate: p.plannedEndDate ?? null,
              notes: p.notes ?? ''
            });
            // Load the linked SO so the line picker + remaining show in the dialog
            this.selectedSo = null;
            this.selectedSoLine = null;
            if (p.salesOrderId) {
              this.soService.getById(p.salesOrderId).subscribe({
                next: (r) => this.zone.run(() => {
                  if (r.success && r.data) {
                    this.selectedSo = r.data;
                    this.selectedSoLine = r.data.lines.find(l => l.id === p.salesOrderLineId) ?? null;
                  }
                  this.cdr.detectChanges();
                })
              });
            }
            this.stages.clear();
            for (const s of p.stages) {
              this.stages.push(this.fb.group({
                stageName: [s.stageName, [Validators.required, Validators.maxLength(100)]],
                plannedQuantity: [s.plannedQuantity as number | null],
                productionLine: [s.productionLine ?? '', Validators.maxLength(100)],
                operatorEmployeeId: [s.operatorEmployeeId as number | null],
                notes: [s.notes ?? '', Validators.maxLength(1000)]
              }));
            }
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
    const stages = (v.stages as any[])
      .filter(s => (s.stageName as string)?.trim())
      .map((s, i) => ({
        sequence: i + 1,
        stageName: (s.stageName as string).trim(),
        plannedQuantity: s.plannedQuantity != null && s.plannedQuantity !== '' ? Number(s.plannedQuantity) : null,
        productionLine: (s.productionLine as string)?.trim() || null,
        operatorEmployeeId: s.operatorEmployeeId ?? null,
        notes: (s.notes as string)?.trim() || null
      }));

    const payload = {
      productId: v.productId,
      bomId: v.bomId,
      quantity: Number(v.quantity) || 0,
      issueWarehouseId: v.issueWarehouseId,
      receiveWarehouseId: v.receiveWarehouseId,
      plannedStartDate: v.plannedStartDate || null,
      plannedEndDate: v.plannedEndDate || null,
      notes: (v.notes as string)?.trim() || null,
      stages,
      salesOrderId: v.salesOrderId ?? null,            // Phase 1 — optional source SO
      salesOrderLineId: v.salesOrderLineId ?? null
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

  /** Raise a draft PR for this order's material shortfalls; jump to PRs on success. */
  generatePr(po: ProductionOrderListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = po.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.poService.generatePr(po.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) this.router.navigate(['/purchase-requisitions']);
        else this.actionError = res.message || 'Could not generate requisition.';   // e.g. "No material shortage…"
        this.cdr.detectChanges();
      }),
      error: (err) => this.handleRowActionError(err)
    });
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

  // ─── Stage tracking (routing progress) ────────────────────────────────────

  hasStages(po: ProductionOrderListItemDto): boolean {
    return po.stageCount > 0;
  }

  openStages(po: ProductionOrderListItemDto): void {
    this.stagesError = '';
    this.completingStageId = null;
    this.trackingOrder = null;
    this.stagesDialogVisible = true;
    this.poService.getById(po.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.trackingOrder = res.data;
          this.cdr.detectChanges();
        });
      }
    });
  }

  startStage(stage: ProductionStageDto): void {
    this.runStageAction(stage.id, this.poService.startStage(stage.id));
  }

  skipStage(stage: ProductionStageDto): void {
    this.runStageAction(stage.id, this.poService.skipStage(stage.id));
  }

  beginComplete(stage: ProductionStageDto): void {
    this.completingStageId = stage.id;
    this.completeQty = stage.completedQuantity || stage.plannedQuantity || 0;
    this.rejectQty = stage.rejectedQuantity || 0;
  }

  cancelComplete(): void {
    this.completingStageId = null;
  }

  confirmComplete(stage: ProductionStageDto): void {
    this.runStageAction(stage.id, this.poService.completeStage(stage.id, {
      completedQuantity: Number(this.completeQty) || 0,
      rejectedQuantity: Number(this.rejectQty) || 0,
      notes: null
    }));
  }

  private runStageAction(stageId: number, obs: any): void {
    if (this.stageActionId) return;
    this.stageActionId = stageId;
    this.stagesError = '';
    this.cdr.detectChanges();
    obs.subscribe({
      next: (res: { success: boolean; message?: string | null; data?: ProductionOrderDto }) => {
        this.zone.run(() => {
          this.stageActionId = null;
          this.completingStageId = null;
          if (res.success && res.data) {
            this.trackingOrder = res.data;
            this.load();   // refresh list progress column
          } else {
            this.stagesError = res.message || 'Stage action failed.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err: any) => {
        this.zone.run(() => {
          this.stageActionId = null;
          this.stagesError = err?.error?.message || 'Stage action failed.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  stageBadgeClass(status: string): string {
    switch (status) {
      case 'Pending':    return 'pending';
      case 'InProgress': return 'inprogress';
      case 'Completed':  return 'completed';
      case 'Skipped':    return 'skipped';
      default:           return '';
    }
  }

  employeeName(id: number | null): string {
    if (!id) return '—';
    return this.employees.find(e => e.id === id)?.fullName ?? '—';
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
