import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DeliveryNoteService } from '../../../services/delivery-note.service';
import { SalesOrderService } from '../../../services/sales-order.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { DN_STATUSES, DeliveryNoteDto, DeliveryNoteListItemDto } from '../../../models/delivery-note.models';
import { SalesOrderDto, SalesOrderListItemDto } from '../../../models/sales-order.models';
import { WarehouseDto } from '../../../models/master-data.models';

interface DispatchableSoOption {
  id: number;
  code: string;
  customerName: string;
  status: string;
  displayLabel: string;
}

@Component({
  selector: 'app-delivery-note-list',
  standalone: false,
  templateUrl: './delivery-note-list.component.html',
  styleUrl: './delivery-note-list.component.scss'
})
export class DeliveryNoteListComponent implements OnInit {

  dns: DeliveryNoteListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterSoId: number | null = null;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = DN_STATUSES;
  dispatchableSos: DispatchableSoOption[] = [];
  allSosForFilter: DispatchableSoOption[] = [];
  warehouses: WarehouseDto[] = [];
  selectedSo: SalesOrderDto | null = null;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deletingDn: DeliveryNoteListItemDto | null = null;
  deleting = false;
  deleteError = '';

  rowActionId: number | null = null;

  constructor(
    private dnService: DeliveryNoteService,
    private soService: SalesOrderService,
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
      salesOrderId: [null as number | null, Validators.required],
      dispatchDate: [this.todayIso(), Validators.required],
      dispatchWarehouseId: [null as number | null, Validators.required],
      vehicleNumber: ['', Validators.maxLength(50)],
      driverContact: ['', Validators.maxLength(100)],
      deliveryAddress: ['', Validators.maxLength(500)],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  private loadDropdowns(): void {
    this.soService.getAll({ page: 1, pageSize: 500, search: '' }).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const mapped: DispatchableSoOption[] = res.data.items.map(s => ({
              id: s.id,
              code: s.code,
              customerName: s.customerName,
              status: s.status,
              displayLabel: `${s.code} — ${s.customerName}`
            }));
            this.allSosForFilter = mapped;
            this.dispatchableSos = mapped.filter(s =>
              s.status === 'Confirmed' || s.status === 'PartiallyDispatched');
          }
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

  load(): void {
    this.loading = true;
    this.dnService.getAll(
      this.parameters,
      this.filterSoId ?? undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.dns = res.data.items;
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

  // ─── SO-driven line building ─────────────────────────────────────────────

  private buildLinesFromSo(so: SalesOrderDto, existingDn?: DeliveryNoteDto): void {
    this.lines.clear();
    for (const soLine of so.lines) {
      const ordered = soLine.quantity;
      const alreadyDispatched = (soLine as any).dispatchedQuantity ?? 0;
      const remaining = ordered - alreadyDispatched;
      const existing = existingDn?.lines.find(l => l.salesOrderLineId === soLine.id);

      if (remaining <= 0 && !existing) continue;

      this.lines.push(this.fb.group({
        salesOrderLineId: [soLine.id, Validators.required],
        productDisplay: [`${soLine.productCode} — ${soLine.productName}`],
        uomCode: [soLine.unitOfMeasureCode],
        orderedQuantity: [ordered],
        alreadyDispatched: [alreadyDispatched],
        remaining: [remaining],
        dispatchedQuantity: [existing?.dispatchedQuantity ?? remaining,
          [Validators.required, Validators.min(0)]],
        lineNotes: [existing?.lineNotes ?? '', Validators.maxLength(1000)]
      }));
    }
  }

  onSoChange(event: any): void {
    const soId = event?.value;
    if (!soId) {
      this.lines.clear();
      this.selectedSo = null;
      return;
    }
    this.fetchSoAndBuildLines(soId, undefined);
  }

  private fetchSoAndBuildLines(soId: number, existingDn?: DeliveryNoteDto): void {
    this.soService.getById(soId).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.selectedSo = res.data;
            this.buildLinesFromSo(res.data, existingDn);
            // If we have an existing DN, also default header from SO where present
            if (!existingDn && !this.form.get('deliveryAddress')?.value) {
              this.form.patchValue({ deliveryAddress: res.data.deliveryAddress ?? '' });
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
    this.selectedSo = null;
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      salesOrderId: this.filterSoId ?? null,
      dispatchDate: this.todayIso(),
      dispatchWarehouseId: this.warehouses[0]?.id ?? null,
      vehicleNumber: '',
      driverContact: '',
      deliveryAddress: '',
      notes: ''
    });
    this.dialogVisible = true;
    if (this.form.get('salesOrderId')?.value) {
      this.fetchSoAndBuildLines(this.form.get('salesOrderId')!.value, undefined);
    }
  }

  openEdit(dn: DeliveryNoteListItemDto): void {
    this.editingId = dn.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.selectedSo = null;
    this.dialogVisible = true;

    this.dnService.getById(dn.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const d = res.data;
            this.dialogMode = d.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              salesOrderId: d.salesOrderId,
              dispatchDate: d.dispatchDate,
              dispatchWarehouseId: d.dispatchWarehouseId,
              vehicleNumber: d.vehicleNumber ?? '',
              driverContact: d.driverContact ?? '',
              deliveryAddress: d.deliveryAddress ?? '',
              notes: d.notes ?? ''
            });

            if (this.dialogMode === 'edit') {
              this.fetchSoAndBuildLines(d.salesOrderId, d);
            } else {
              this.buildLinesFromDnReadonly(d);
              this.form.disable();
            }
            this.form.get('salesOrderId')?.disable();
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  private buildLinesFromDnReadonly(dn: DeliveryNoteDto): void {
    this.lines.clear();
    for (const l of dn.lines) {
      this.lines.push(this.fb.group({
        salesOrderLineId: [l.salesOrderLineId],
        productDisplay: [`${l.productCode} — ${l.productName}`],
        uomCode: [l.unitOfMeasureCode],
        orderedQuantity: [l.orderedQuantity],
        alreadyDispatched: [0],
        remaining: [0],
        dispatchedQuantity: [l.dispatchedQuantity],
        lineNotes: [l.lineNotes ?? '']
      }));
    }
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving || this.dialogMode === 'view') return;

    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();
    const lines = (v.lines as any[])
      .filter(l => Number(l.dispatchedQuantity) > 0)
      .map(l => ({
        salesOrderLineId: l.salesOrderLineId,
        dispatchedQuantity: Number(l.dispatchedQuantity),
        lineNotes: (l.lineNotes as string)?.trim() || null
      }));

    if (lines.length === 0) {
      this.dialogSaving = false;
      this.dialogError = 'Enter at least one dispatched quantity (> 0).';
      this.cdr.detectChanges();
      return;
    }

    const baseFields = {
      dispatchDate: v.dispatchDate,
      dispatchWarehouseId: v.dispatchWarehouseId,
      vehicleNumber: (v.vehicleNumber as string)?.trim() || null,
      driverContact: (v.driverContact as string)?.trim() || null,
      deliveryAddress: (v.deliveryAddress as string)?.trim() || null,
      notes: (v.notes as string)?.trim() || null,
      lines
    };

    if (this.dialogMode === 'create') {
      this.dnService.create({ salesOrderId: v.salesOrderId, ...baseFields }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.dnService.update(this.editingId, baseFields).subscribe({
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

  post(dn: DeliveryNoteListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = dn.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.dnService.post(dn.id).subscribe({
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

  confirmDelete(dn: DeliveryNoteListItemDto): void {
    this.deletingDn = dn;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingDn || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.dnService.delete(this.deletingDn.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingDn = null;
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

  formatDate(d: string | null): string {
    return d ? d : '—';
  }
}
