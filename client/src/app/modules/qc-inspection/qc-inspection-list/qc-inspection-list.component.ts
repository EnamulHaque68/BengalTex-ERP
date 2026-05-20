import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { QcInspectionService } from '../../../services/qc-inspection.service';
import { GoodsReceiptService } from '../../../services/goods-receipt.service';
import { ProductionOrderService } from '../../../services/production-order.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  QC_SOURCE_TYPES,
  QC_STATUSES,
  QcInspectionListItemDto
} from '../../../models/qc-inspection.models';
import { GoodsReceiptListItemDto } from '../../../models/goods-receipt.models';
import { ProductionOrderListItemDto } from '../../../models/production-order.models';
import { WarehouseDto } from '../../../models/master-data.models';

interface SourceOption {
  id: number;
  code: string;
  displayLabel: string;
}

@Component({
  selector: 'app-qc-inspection-list',
  standalone: false,
  templateUrl: './qc-inspection-list.component.html',
  styleUrl: './qc-inspection-list.component.scss'
})
export class QcInspectionListComponent implements OnInit {

  inspections: QcInspectionListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterSourceType: string | null = null;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly sourceTypes = QC_SOURCE_TYPES;
  readonly statuses = QC_STATUSES;
  warehouses: WarehouseDto[] = [];
  postedGrns: SourceOption[] = [];
  completedProductions: SourceOption[] = [];

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deletingInsp: QcInspectionListItemDto | null = null;
  deleting = false;
  deleteError = '';

  rowActionId: number | null = null;

  constructor(
    private qcService: QcInspectionService,
    private grnService: GoodsReceiptService,
    private prodService: ProductionOrderService,
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
      sourceType: ['IncomingMaterial', Validators.required],
      sourceId: [null as number | null, Validators.required],
      quarantineWarehouseId: [null as number | null, Validators.required],
      inspectionDate: [this.todayIso(), Validators.required],
      inspectedBy: ['', Validators.maxLength(100)],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  get sourceType(): string {
    return this.form.get('sourceType')?.value;
  }

  get sourceOptions(): SourceOption[] {
    return this.sourceType === 'IncomingMaterial' ? this.postedGrns : this.completedProductions;
  }

  private loadDropdowns(): void {
    this.warehouseService.getAll(undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.warehouses = res.data;
          this.cdr.detectChanges();
        });
      }
    });
    this.grnService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, 'Posted').subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.postedGrns = res.data.items
              .filter((g: GoodsReceiptListItemDto) => g.status === 'Posted')
              .map((g: GoodsReceiptListItemDto) => ({
                id: g.id, code: g.code,
                displayLabel: `${g.code} — ${g.supplierName} (${g.receiveDate})`
              }));
          }
          this.cdr.detectChanges();
        });
      }
    });
    this.prodService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, 'Completed').subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.completedProductions = res.data.items
              .filter((p: ProductionOrderListItemDto) => p.status === 'Completed')
              .map((p: ProductionOrderListItemDto) => ({
                id: p.id, code: p.code,
                displayLabel: `${p.code} — ${p.productName} (${p.quantity})`
              }));
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.qcService.getAll(
      this.parameters,
      this.filterSourceType ?? undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.inspections = res.data.items;
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

  private newLine(
    rawMaterialId: number | null, productId: number | null, itemDisplay: string, uomCode: string,
    inspected: number, passed: number, defectNotes = ''
  ): FormGroup {
    return this.fb.group({
      rawMaterialId: [rawMaterialId],
      productId: [productId],
      itemDisplay: [itemDisplay],
      uomCode: [uomCode],
      inspectedQuantity: [inspected, [Validators.required, Validators.min(0.0001)]],
      passedQuantity: [passed, [Validators.required, Validators.min(0)]],
      defectNotes: [defectNotes, Validators.maxLength(1000)]
    });
  }

  rejectedQty(line: AbstractControl): number {
    const insp = Number(line.get('inspectedQuantity')?.value) || 0;
    const passed = Number(line.get('passedQuantity')?.value) || 0;
    return Math.max(0, insp - passed);
  }

  onSourceTypeChange(): void {
    this.form.patchValue({ sourceId: null });
    this.lines.clear();
  }

  onSourceChange(event: any): void {
    const id = event?.value;
    this.lines.clear();
    if (!id) return;

    if (this.sourceType === 'IncomingMaterial') {
      this.grnService.getById(id).subscribe({
        next: (res) => {
          this.zone.run(() => {
            if (res.success && res.data) {
              for (const l of res.data.lines) {
                const remaining = l.receivedQuantity - l.returnedQuantity;
                this.lines.push(this.newLine(
                  l.rawMaterialId, null,
                  `${l.rawMaterialCode} — ${l.rawMaterialName}`,
                  l.unitOfMeasureCode,
                  remaining, remaining, ''
                ));
              }
            }
            this.cdr.detectChanges();
          });
        }
      });
    } else {
      this.prodService.getById(id).subscribe({
        next: (res) => {
          this.zone.run(() => {
            if (res.success && res.data) {
              const p = res.data;
              this.lines.push(this.newLine(
                null, p.productId,
                `${p.productCode} — ${p.productName}`,
                p.productUnitOfMeasureCode ?? '',
                p.quantity, p.quantity, ''
              ));
            }
            this.cdr.detectChanges();
          });
        }
      });
    }
  }

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      sourceType: 'IncomingMaterial',
      sourceId: null,
      quarantineWarehouseId: null,
      inspectionDate: this.todayIso(),
      inspectedBy: '',
      notes: ''
    });
    this.dialogVisible = true;
  }

  openEdit(i: QcInspectionListItemDto): void {
    this.editingId = i.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.dialogVisible = true;

    this.qcService.getById(i.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const q = res.data;
            this.dialogMode = q.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              sourceType: q.sourceType,
              sourceId: q.goodsReceiptNoteId ?? q.productionOrderId,
              quarantineWarehouseId: q.quarantineWarehouseId,
              inspectionDate: q.inspectionDate,
              inspectedBy: q.inspectedBy ?? '',
              notes: q.notes ?? ''
            });
            for (const l of q.lines) {
              this.lines.push(this.newLine(
                l.rawMaterialId, l.productId,
                `${l.itemCode} — ${l.itemName}`,
                l.unitOfMeasureCode,
                l.inspectedQuantity, l.passedQuantity, l.defectNotes ?? ''
              ));
            }
            this.form.get('sourceType')?.disable();
            this.form.get('sourceId')?.disable();
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
    const lines = (v.lines as any[]).map(l => ({
      rawMaterialId: l.rawMaterialId,
      productId: l.productId,
      inspectedQuantity: Number(l.inspectedQuantity) || 0,
      passedQuantity: Number(l.passedQuantity) || 0,
      defectNotes: (l.defectNotes as string)?.trim() || null
    }));

    if (this.dialogMode === 'create') {
      this.qcService.create({
        sourceType: v.sourceType,
        goodsReceiptNoteId: v.sourceType === 'IncomingMaterial' ? v.sourceId : null,
        productionOrderId: v.sourceType === 'FinishedGoods' ? v.sourceId : null,
        inspectionDate: v.inspectionDate,
        quarantineWarehouseId: v.quarantineWarehouseId,
        inspectedBy: (v.inspectedBy as string)?.trim() || null,
        notes: (v.notes as string)?.trim() || null,
        lines
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.qcService.update(this.editingId, {
        inspectionDate: v.inspectionDate,
        quarantineWarehouseId: v.quarantineWarehouseId,
        inspectedBy: (v.inspectedBy as string)?.trim() || null,
        notes: (v.notes as string)?.trim() || null,
        lines
      }).subscribe({
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

  post(i: QcInspectionListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = i.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.qcService.post(i.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.rowActionId = null;
          if (res.success) {
            this.actionError = '';
            this.load();
          } else {
            this.actionError = res.message || 'Post failed.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.rowActionId = null;
          this.actionError = err?.error?.message || 'Post failed.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  canDelete(i: QcInspectionListItemDto): boolean {
    return i.status === 'Draft';
  }

  confirmDelete(i: QcInspectionListItemDto): void {
    this.deletingInsp = i;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingInsp || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();
    this.qcService.delete(this.deletingInsp.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingInsp = null;
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

  resultClass(result: string): string {
    switch (result) {
      case 'Passed':          return 'passed';
      case 'PartiallyPassed': return 'partial';
      case 'Failed':          return 'failed';
      default:                return '';
    }
  }

  resultLabel(result: string): string {
    return result === 'PartiallyPassed' ? 'Partial' : result;
  }

  sourceTypeLabel(value: string): string {
    return value === 'IncomingMaterial' ? 'Incoming' : 'Finished Goods';
  }

  meta(line: AbstractControl) {
    return line.getRawValue();
  }

  totalRejected(): number {
    return this.lines.controls.reduce((sum, l) => sum + this.rejectedQty(l), 0);
  }

  formatNumber(n: number): string {
    return new Intl.NumberFormat('en-BD', { maximumFractionDigits: 4 }).format(n || 0);
  }
}
