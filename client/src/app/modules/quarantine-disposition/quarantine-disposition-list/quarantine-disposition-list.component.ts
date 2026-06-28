import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { QuarantineDispositionService } from '../../../services/quarantine-disposition.service';
import { StockService } from '../../../services/stock.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  DISPOSITION_TYPES,
  DISPOSITION_STATUSES,
  QuarantineDispositionListItemDto
} from '../../../models/quarantine-disposition.models';
import { WarehouseDto } from '../../../models/master-data.models';

@Component({
  selector: 'app-quarantine-disposition-list',
  standalone: false,
  templateUrl: './quarantine-disposition-list.component.html',
  styleUrl: './quarantine-disposition-list.component.scss'
})
export class QuarantineDispositionListComponent implements OnInit {

  dispositions: QuarantineDispositionListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterType: string | null = null;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly dispositionTypes = DISPOSITION_TYPES;
  readonly statuses = DISPOSITION_STATUSES;
  warehouses: WarehouseDto[] = [];

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  loadingStock = false;
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deletingDisp: QuarantineDispositionListItemDto | null = null;
  deleting = false;
  deleteError = '';

  rowActionId: number | null = null;

  constructor(
    private dispService: QuarantineDispositionService,
    private stockService: StockService,
    private warehouseService: WarehouseService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadWarehouses();
    this.load();
  }

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private buildForm(): void {
    this.form = this.fb.group({
      dispositionType: ['Release', Validators.required],
      quarantineWarehouseId: [null as number | null, Validators.required],
      destinationWarehouseId: [null as number | null],
      dispositionDate: [this.todayIso(), Validators.required],
      reason: ['', Validators.maxLength(500)],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  get dispositionType(): string {
    return this.form.get('dispositionType')?.value;
  }

  get isRelease(): boolean {
    return this.dispositionType === 'Release';
  }

  /** Release and Rework both move stock to a destination warehouse; Scrap does not. */
  get needsDestination(): boolean {
    return this.dispositionType === 'Release' || this.dispositionType === 'Rework';
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
    this.dispService.getAll(
      this.parameters,
      this.filterType ?? undefined,
      undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.dispositions = res.data.items;
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
    available: number, quantity: number, lineNotes = ''
  ): FormGroup {
    return this.fb.group({
      rawMaterialId: [rawMaterialId],
      productId: [productId],
      itemDisplay: [itemDisplay],
      uomCode: [uomCode],
      availableInQuarantine: [available],
      quantity: [quantity, [Validators.min(0), Validators.max(available)]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  // Load the items currently sitting in the chosen quarantine warehouse as candidate lines
  onQuarantineChange(): void {
    const whId = this.form.get('quarantineWarehouseId')?.value;
    this.lines.clear();
    if (!whId) { this.cdr.detectChanges(); return; }

    this.loadingStock = true;
    this.cdr.detectChanges();
    this.stockService.getOnHand({ page: 1, pageSize: 500, search: '' }, whId).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loadingStock = false;
          if (res.success && res.data) {
            for (const s of res.data.items) {
              if (s.quantity <= 0) continue;
              this.lines.push(this.newLine(
                s.rawMaterialId, s.productId,
                `${s.itemCode} — ${s.itemName}`,
                s.unitOfMeasureCode,
                s.quantity, 0, ''
              ));
            }
          }
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => { this.loadingStock = false; this.cdr.detectChanges(); });
      }
    });
  }

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      dispositionType: 'Release',
      quarantineWarehouseId: null,
      destinationWarehouseId: null,
      dispositionDate: this.todayIso(),
      reason: '',
      notes: ''
    });
    this.dialogVisible = true;
  }

  openEdit(d: QuarantineDispositionListItemDto): void {
    this.editingId = d.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.dialogVisible = true;

    this.dispService.getById(d.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const disp = res.data;
            this.dialogMode = disp.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              dispositionType: disp.dispositionType,
              quarantineWarehouseId: disp.quarantineWarehouseId,
              destinationWarehouseId: disp.destinationWarehouseId,
              dispositionDate: disp.dispositionDate,
              reason: disp.reason ?? '',
              notes: disp.notes ?? ''
            });
            for (const l of disp.lines) {
              this.lines.push(this.newLine(
                l.rawMaterialId, l.productId,
                `${l.itemCode} — ${l.itemName}`,
                l.unitOfMeasureCode,
                l.availableInQuarantine, l.quantity, l.lineNotes ?? ''
              ));
            }
            this.form.get('dispositionType')?.disable();
            this.form.get('quarantineWarehouseId')?.disable();
            if (this.dialogMode === 'view') this.form.disable();
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving || this.dialogMode === 'view') return;
    if (this.needsDestination && !this.form.get('destinationWarehouseId')?.value) {
      this.dialogError = `Pick a destination warehouse for a ${this.dispositionType} disposition.`;
      this.cdr.detectChanges();
      return;
    }

    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();
    const lines = (v.lines as any[])
      .filter(l => Number(l.quantity) > 0)
      .map(l => ({
        rawMaterialId: l.rawMaterialId,
        productId: l.productId,
        quantity: Number(l.quantity) || 0,
        lineNotes: (l.lineNotes as string)?.trim() || null
      }));

    if (lines.length === 0) {
      this.dialogSaving = false;
      this.dialogError = 'Enter a dispose quantity for at least one line.';
      this.cdr.detectChanges();
      return;
    }

    if (this.dialogMode === 'create') {
      this.dispService.create({
        dispositionType: v.dispositionType,
        dispositionDate: v.dispositionDate,
        quarantineWarehouseId: v.quarantineWarehouseId,
        destinationWarehouseId: (v.dispositionType === 'Release' || v.dispositionType === 'Rework') ? v.destinationWarehouseId : null,
        reason: (v.reason as string)?.trim() || null,
        notes: (v.notes as string)?.trim() || null,
        lines
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.dispService.update(this.editingId, {
        dispositionDate: v.dispositionDate,
        destinationWarehouseId: (v.dispositionType === 'Release' || v.dispositionType === 'Rework') ? v.destinationWarehouseId : null,
        reason: (v.reason as string)?.trim() || null,
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

  post(d: QuarantineDispositionListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = d.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.dispService.post(d.id).subscribe({
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

  canDelete(d: QuarantineDispositionListItemDto): boolean {
    return d.status === 'Draft';
  }

  confirmDelete(d: QuarantineDispositionListItemDto): void {
    this.deletingDisp = d;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingDisp || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();
    this.dispService.delete(this.deletingDisp.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingDisp = null;
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

  typeLabel(value: string): string {
    return value;
  }

  meta(line: AbstractControl) {
    return line.getRawValue();
  }

  totalDisposeQty(): number {
    return this.lines.controls.reduce((sum, l) => sum + (Number(l.get('quantity')?.value) || 0), 0);
  }

  formatNumber(n: number): string {
    return new Intl.NumberFormat('en-BD', { maximumFractionDigits: 4 }).format(n || 0);
  }
}
