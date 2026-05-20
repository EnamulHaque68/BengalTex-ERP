import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SupplierReturnNoteService } from '../../../services/supplier-return-note.service';
import { GoodsReceiptService } from '../../../services/goods-receipt.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  SRN_STATUSES,
  SupplierReturnNoteListItemDto
} from '../../../models/supplier-return-note.models';
import { GoodsReceiptDto, GoodsReceiptListItemDto } from '../../../models/goods-receipt.models';
import { WarehouseDto } from '../../../models/master-data.models';

interface PostedGrnOption {
  id: number;
  code: string;
  supplierName: string;
  displayLabel: string;
}

@Component({
  selector: 'app-supplier-return-note-list',
  standalone: false,
  templateUrl: './supplier-return-note-list.component.html',
  styleUrl: './supplier-return-note-list.component.scss'
})
export class SupplierReturnNoteListComponent implements OnInit {

  srns: SupplierReturnNoteListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = SRN_STATUSES;
  warehouses: WarehouseDto[] = [];
  postedGrns: PostedGrnOption[] = [];
  selectedGrn: GoodsReceiptDto | null = null;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deletingSrn: SupplierReturnNoteListItemDto | null = null;
  deleting = false;
  deleteError = '';

  rowActionId: number | null = null;

  constructor(
    private srnService: SupplierReturnNoteService,
    private grnService: GoodsReceiptService,
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
      goodsReceiptNoteId: [null as number | null, Validators.required],
      returnFromWarehouseId: [null as number | null, Validators.required],
      returnDate: [this.todayIso(), Validators.required],
      vehicleNumber: ['', Validators.maxLength(50)],
      reason: ['', Validators.maxLength(500)],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
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
                id: g.id,
                code: g.code,
                supplierName: g.supplierName,
                displayLabel: `${g.code} — ${g.supplierName} (${g.receiveDate})`
              }));
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.srnService.getAll(
      this.parameters,
      undefined,
      undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.srns = res.data.items;
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
    goodsReceiptLineId: number, rmDisplay: string, uomCode: string,
    received: number, alreadyReturned: number, returnQty: number, lineNotes = ''
  ): FormGroup {
    const available = received - alreadyReturned;
    return this.fb.group({
      goodsReceiptLineId: [goodsReceiptLineId],
      rmDisplay: [rmDisplay],
      uomCode: [uomCode],
      receivedQuantity: [received],
      alreadyReturnedQuantity: [alreadyReturned],
      availableForReturn: [available],
      returnedQuantity: [returnQty, [Validators.min(0), Validators.max(available)]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  onGrnChange(event: any): void {
    const grnId = event?.value;
    this.lines.clear();
    this.selectedGrn = null;
    if (!grnId) return;
    this.grnService.getById(grnId).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.selectedGrn = res.data;
            for (const grnLine of res.data.lines) {
              this.lines.push(this.newLine(
                grnLine.id,
                `${grnLine.rawMaterialCode} — ${grnLine.rawMaterialName}`,
                grnLine.unitOfMeasureCode,
                grnLine.receivedQuantity,
                grnLine.returnedQuantity,
                0,
                ''
              ));
            }
            // Default the return-from warehouse to the GRN's receiving warehouse if not set yet
            if (!this.form.get('returnFromWarehouseId')?.value && res.data.receivingWarehouseId) {
              this.form.patchValue({ returnFromWarehouseId: res.data.receivingWarehouseId });
            }
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.selectedGrn = null;
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      goodsReceiptNoteId: null,
      returnFromWarehouseId: null,
      returnDate: this.todayIso(),
      vehicleNumber: '',
      reason: '',
      notes: ''
    });
    this.dialogVisible = true;
  }

  openEdit(s: SupplierReturnNoteListItemDto): void {
    this.editingId = s.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.selectedGrn = null;
    this.dialogVisible = true;

    this.srnService.getById(s.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const srn = res.data;
            this.dialogMode = srn.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              goodsReceiptNoteId: srn.goodsReceiptNoteId,
              returnFromWarehouseId: srn.returnFromWarehouseId,
              returnDate: srn.returnDate,
              vehicleNumber: srn.vehicleNumber ?? '',
              reason: srn.reason ?? '',
              notes: srn.notes ?? ''
            });
            for (const l of srn.lines) {
              this.lines.push(this.newLine(
                l.goodsReceiptLineId,
                `${l.rawMaterialCode} — ${l.rawMaterialName}`,
                l.unitOfMeasureCode,
                l.receivedQuantity,
                l.previouslyReturnedQuantity,
                l.returnedQuantity,
                l.lineNotes ?? ''
              ));
            }
            this.form.get('goodsReceiptNoteId')?.disable();
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
      .filter(l => Number(l.returnedQuantity) > 0)
      .map(l => ({
        goodsReceiptLineId: l.goodsReceiptLineId,
        returnedQuantity: Number(l.returnedQuantity) || 0,
        lineNotes: (l.lineNotes as string)?.trim() || null
      }));

    if (lines.length === 0) {
      this.dialogSaving = false;
      this.dialogError = 'Enter a return quantity for at least one line.';
      this.cdr.detectChanges();
      return;
    }

    if (this.dialogMode === 'create') {
      this.srnService.create({
        goodsReceiptNoteId: v.goodsReceiptNoteId,
        returnFromWarehouseId: v.returnFromWarehouseId,
        returnDate: v.returnDate,
        vehicleNumber: (v.vehicleNumber as string)?.trim() || null,
        reason: (v.reason as string)?.trim() || null,
        notes: (v.notes as string)?.trim() || null,
        lines
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.srnService.update(this.editingId, {
        returnFromWarehouseId: v.returnFromWarehouseId,
        returnDate: v.returnDate,
        vehicleNumber: (v.vehicleNumber as string)?.trim() || null,
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

  post(s: SupplierReturnNoteListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = s.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.srnService.post(s.id).subscribe({
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

  canDelete(s: SupplierReturnNoteListItemDto): boolean {
    return s.status === 'Draft';
  }

  confirmDelete(s: SupplierReturnNoteListItemDto): void {
    this.deletingSrn = s;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingSrn || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();
    this.srnService.delete(this.deletingSrn.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingSrn = null;
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

  meta(line: AbstractControl) {
    return line.getRawValue();
  }

  totalReturnedQty(): number {
    return this.lines.controls.reduce(
      (sum, l) => sum + (Number(l.get('returnedQuantity')?.value) || 0), 0);
  }

  formatNumber(n: number): string {
    return new Intl.NumberFormat('en-BD', { maximumFractionDigits: 4 }).format(n || 0);
  }
}
