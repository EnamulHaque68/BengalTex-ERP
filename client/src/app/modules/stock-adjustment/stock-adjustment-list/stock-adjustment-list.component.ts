import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { StockAdjustmentService } from '../../../services/stock-adjustment.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  STOCK_ADJUSTMENT_STATUSES,
  StockAdjustmentListItemDto
} from '../../../models/inventory.models';
import { WarehouseDto } from '../../../models/master-data.models';
import { RawMaterialListItemDto } from '../../../models/raw-material.models';

@Component({
  selector: 'app-stock-adjustment-list',
  standalone: false,
  templateUrl: './stock-adjustment-list.component.html',
  styleUrl: './stock-adjustment-list.component.scss'
})
export class StockAdjustmentListComponent implements OnInit {

  adjustments: StockAdjustmentListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterWarehouseId: number | null = null;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = STOCK_ADJUSTMENT_STATUSES;
  warehouses: WarehouseDto[] = [];
  rawMaterials: RawMaterialListItemDto[] = [];

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingAdj: StockAdjustmentListItemDto | null = null;
  deleting = false;
  deleteError = '';

  // Row action (post) in-flight id
  rowActionId: number | null = null;

  constructor(
    private adjService: StockAdjustmentService,
    private warehouseService: WarehouseService,
    private rawMaterialService: RawMaterialService,
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
      adjustmentDate: [this.todayIso(), Validators.required],
      warehouseId: [null as number | null, Validators.required],
      reason: ['', [Validators.required, Validators.maxLength(200)]],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  private newLine(rawMaterialId: number | null = null, signedQuantity: number | null = null, lineNotes = ''): FormGroup {
    return this.fb.group({
      rawMaterialId: [rawMaterialId, Validators.required],
      signedQuantity: [signedQuantity, [Validators.required]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  addLine(): void {
    this.lines.push(this.newLine());
  }

  removeLine(index: number): void {
    this.lines.removeAt(index);
  }

  rawMaterialById(id: number | null | undefined): RawMaterialListItemDto | undefined {
    return id ? this.rawMaterials.find(r => r.id === id) : undefined;
  }

  lineUomCode(line: AbstractControl): string {
    return this.rawMaterialById(line.get('rawMaterialId')?.value)?.unitOfMeasureCode ?? '—';
  }

  // ─── Data loading ────────────────────────────────────────────────────────

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
    this.adjService.getAll(
      this.parameters,
      this.filterWarehouseId ?? undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.adjustments = res.data.items;
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
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      adjustmentDate: this.todayIso(),
      warehouseId: this.filterWarehouseId ?? this.warehouses[0]?.id ?? null,
      reason: '',
      notes: ''
    });
    this.addLine();
    this.dialogVisible = true;
  }

  openEdit(adj: StockAdjustmentListItemDto): void {
    this.editingId = adj.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.dialogVisible = true;

    this.adjService.getById(adj.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const a = res.data;
            this.dialogMode = a.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              adjustmentDate: a.adjustmentDate,
              warehouseId: a.warehouseId,
              reason: a.reason,
              notes: a.notes ?? ''
            });
            a.lines.forEach(l => this.lines.push(
              this.newLine(l.rawMaterialId, l.signedQuantity, l.lineNotes ?? '')
            ));
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
    const lines = (v.lines as any[])
      .filter(l => l.rawMaterialId && Number(l.signedQuantity) !== 0)
      .map(l => ({
        rawMaterialId: l.rawMaterialId,
        signedQuantity: Number(l.signedQuantity),
        lineNotes: (l.lineNotes as string)?.trim() || null
      }));

    if (lines.length === 0) {
      this.dialogSaving = false;
      this.dialogError = 'Add at least one line with a non-zero quantity.';
      this.cdr.detectChanges();
      return;
    }

    const baseFields = {
      adjustmentDate: v.adjustmentDate,
      warehouseId: v.warehouseId,
      reason: (v.reason as string).trim(),
      notes: (v.notes as string)?.trim() || null,
      lines
    };

    if (this.dialogMode === 'create') {
      this.adjService.create(baseFields).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.adjService.update(this.editingId, baseFields).subscribe({
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

  // ─── Row actions: post ───────────────────────────────────────────────────

  post(adj: StockAdjustmentListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = adj.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.adjService.post(adj.id).subscribe({
      next: (res) => this.handleRowAction(res),
      error: (err) => this.handleRowActionError(err)
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

  // ─── Delete ──────────────────────────────────────────────────────────────

  confirmDelete(adj: StockAdjustmentListItemDto): void {
    this.deletingAdj = adj;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingAdj || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.adjService.delete(this.deletingAdj.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingAdj = null;
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
}
