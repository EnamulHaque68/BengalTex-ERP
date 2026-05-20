import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CustomerReturnNoteService } from '../../../services/customer-return-note.service';
import { DeliveryNoteService } from '../../../services/delivery-note.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  CRN_STATUSES,
  CustomerReturnNoteListItemDto
} from '../../../models/customer-return-note.models';
import { DeliveryNoteDto, DeliveryNoteListItemDto } from '../../../models/delivery-note.models';
import { WarehouseDto } from '../../../models/master-data.models';

interface PostedDnOption {
  id: number;
  code: string;
  customerName: string;
  displayLabel: string;
}

@Component({
  selector: 'app-customer-return-note-list',
  standalone: false,
  templateUrl: './customer-return-note-list.component.html',
  styleUrl: './customer-return-note-list.component.scss'
})
export class CustomerReturnNoteListComponent implements OnInit {

  crns: CustomerReturnNoteListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = CRN_STATUSES;
  warehouses: WarehouseDto[] = [];
  postedDns: PostedDnOption[] = [];
  selectedDn: DeliveryNoteDto | null = null;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deletingCrn: CustomerReturnNoteListItemDto | null = null;
  deleting = false;
  deleteError = '';

  rowActionId: number | null = null;

  constructor(
    private crnService: CustomerReturnNoteService,
    private dnService: DeliveryNoteService,
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
      deliveryNoteId: [null as number | null, Validators.required],
      returnWarehouseId: [null as number | null, Validators.required],
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
    this.dnService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, 'Posted').subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.postedDns = res.data.items
              .filter((d: DeliveryNoteListItemDto) => d.status === 'Posted')
              .map((d: DeliveryNoteListItemDto) => ({
                id: d.id,
                code: d.code,
                customerName: d.customerName,
                displayLabel: `${d.code} — ${d.customerName} (${d.dispatchDate})`
              }));
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.crnService.getAll(
      this.parameters,
      undefined,
      undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.crns = res.data.items;
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
    deliveryNoteLineId: number, productDisplay: string, uomCode: string,
    dispatched: number, alreadyReturned: number, returnQty: number, lineNotes = ''
  ): FormGroup {
    const available = dispatched - alreadyReturned;
    return this.fb.group({
      deliveryNoteLineId: [deliveryNoteLineId],
      productDisplay: [productDisplay],
      uomCode: [uomCode],
      dispatchedQuantity: [dispatched],
      alreadyReturnedQuantity: [alreadyReturned],
      availableForReturn: [available],
      returnedQuantity: [returnQty, [Validators.min(0), Validators.max(available)]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  onDnChange(event: any): void {
    const dnId = event?.value;
    this.lines.clear();
    this.selectedDn = null;
    if (!dnId) return;
    this.dnService.getById(dnId).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.selectedDn = res.data;
            // Populate one row per DN line; user enters qty > 0 for the ones to return
            for (const dnLine of res.data.lines) {
              this.lines.push(this.newLine(
                dnLine.id,
                `${dnLine.productCode} — ${dnLine.productName}`,
                dnLine.unitOfMeasureCode,
                dnLine.dispatchedQuantity,
                dnLine.returnedQuantity,
                0,
                ''
              ));
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
    this.selectedDn = null;
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      deliveryNoteId: null,
      returnWarehouseId: null,
      returnDate: this.todayIso(),
      vehicleNumber: '',
      reason: '',
      notes: ''
    });
    this.dialogVisible = true;
  }

  openEdit(c: CustomerReturnNoteListItemDto): void {
    this.editingId = c.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.selectedDn = null;
    this.dialogVisible = true;

    this.crnService.getById(c.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const crn = res.data;
            this.dialogMode = crn.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              deliveryNoteId: crn.deliveryNoteId,
              returnWarehouseId: crn.returnWarehouseId,
              returnDate: crn.returnDate,
              vehicleNumber: crn.vehicleNumber ?? '',
              reason: crn.reason ?? '',
              notes: crn.notes ?? ''
            });
            for (const l of crn.lines) {
              this.lines.push(this.newLine(
                l.deliveryNoteLineId,
                `${l.productCode} — ${l.productName}`,
                l.unitOfMeasureCode,
                l.dispatchedQuantity,
                l.previouslyReturnedQuantity,
                l.returnedQuantity,
                l.lineNotes ?? ''
              ));
            }
            this.form.get('deliveryNoteId')?.disable();
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
    // Filter out lines with qty=0 (user didn't want to return these)
    const lines = (v.lines as any[])
      .filter(l => Number(l.returnedQuantity) > 0)
      .map(l => ({
        deliveryNoteLineId: l.deliveryNoteLineId,
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
      this.crnService.create({
        deliveryNoteId: v.deliveryNoteId,
        returnWarehouseId: v.returnWarehouseId,
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
      this.crnService.update(this.editingId, {
        returnWarehouseId: v.returnWarehouseId,
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

  post(c: CustomerReturnNoteListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = c.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.crnService.post(c.id).subscribe({
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

  canDelete(c: CustomerReturnNoteListItemDto): boolean {
    return c.status === 'Draft';
  }

  confirmDelete(c: CustomerReturnNoteListItemDto): void {
    this.deletingCrn = c;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingCrn || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();
    this.crnService.delete(this.deletingCrn.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingCrn = null;
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
