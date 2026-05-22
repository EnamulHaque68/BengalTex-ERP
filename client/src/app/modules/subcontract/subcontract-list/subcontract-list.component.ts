import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SubcontractService } from '../../../services/subcontract.service';
import { SupplierService } from '../../../services/supplier.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { ProductService } from '../../../services/product.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  SubcontractOrderDto, SubcontractOrderListItemDto, SUBCONTRACT_STATUSES
} from '../../../models/subcontract.models';
import { SupplierListItemDto } from '../../../models/supplier.models';
import { WarehouseDto } from '../../../models/master-data.models';

interface ItemOption { key: string; label: string; uom: string; }

@Component({
  selector: 'app-subcontract-list',
  standalone: false,
  templateUrl: './subcontract-list.component.html',
  styleUrl: './subcontract-list.component.scss'
})
export class SubcontractListComponent implements OnInit {

  orders: SubcontractOrderListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;
  rowActionId: number | null = null;

  readonly statuses = SUBCONTRACT_STATUSES;
  suppliers: SupplierListItemDto[] = [];
  warehouses: WarehouseDto[] = [];
  itemOptions: ItemOption[] = [];

  // Create / edit dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Receive dialog
  receiveVisible = false;
  receiveSaving = false;
  receiveError = '';
  receiveOrder: SubcontractOrderDto | null = null;
  receiveForm!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deleting = false;
  deleteError = '';
  deletingOrder: SubcontractOrderListItemDto | null = null;

  constructor(
    private service: SubcontractService,
    private supplierService: SupplierService,
    private warehouseService: WarehouseService,
    private rawMaterialService: RawMaterialService,
    private productService: ProductService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadDropdowns();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  private buildForm(): void {
    this.form = this.fb.group({
      subcontractorId: [null as number | null, Validators.required],
      warehouseId: [null as number | null, Validators.required],
      processType: ['', [Validators.required, Validators.maxLength(100)]],
      orderDate: [this.todayIso(), Validators.required],
      expectedReturnDate: [null as string | null],
      chargeAmount: [0, [Validators.required, Validators.min(0)]],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }

  private newLine(itemKey: string | null = null, issuedQuantity = 1, lineNotes = ''): FormGroup {
    return this.fb.group({
      itemKey: [itemKey, Validators.required],
      issuedQuantity: [issuedQuantity, [Validators.required, Validators.min(0.0001)]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  addLine(): void { this.lines.push(this.newLine()); }
  removeLine(i: number): void { this.lines.removeAt(i); }

  itemUom(itemKey: string | null | undefined): string {
    return this.itemOptions.find(o => o.key === itemKey)?.uom ?? '—';
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }

  formatDate(d: string | null): string { return d || '—'; }

  private loadDropdowns(): void {
    this.supplierService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.suppliers = res.data.items; this.cdr.detectChanges(); })
    });
    this.warehouseService.getAll(undefined, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success) this.warehouses = res.data ?? []; this.cdr.detectChanges(); })
    });
    this.rawMaterialService.getAll({ page: 1, pageSize: 1000, search: '' }, undefined, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const rms = res.data.items.map(r => ({ key: 'R:' + r.id, label: `${r.code} — ${r.name} (RM)`, uom: r.unitOfMeasureCode }));
          this.itemOptions = [...this.itemOptions.filter(o => !o.key.startsWith('R:')), ...rms];
        }
        this.cdr.detectChanges();
      })
    });
    this.productService.getAll({ page: 1, pageSize: 1000, search: '' }, undefined, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const ps = res.data.items.map(p => ({ key: 'P:' + p.id, label: `${p.code} — ${p.name} (FG)`, uom: p.unitOfMeasureCode }));
          this.itemOptions = [...this.itemOptions.filter(o => !o.key.startsWith('P:')), ...ps];
        }
        this.cdr.detectChanges();
      })
    });
  }

  load(): void {
    this.loading = true;
    this.service.getAll(this.parameters, undefined, this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.orders = res.data.items; this.totalCount = res.data.totalCount; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearchChange(value: string): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.parameters.search = value; this.parameters.page = 1; this.load(); }, 400);
  }

  onPageChange(event: any): void {
    this.parameters.page = Math.floor(event.first / event.rows) + 1;
    this.parameters.pageSize = event.rows;
    this.load();
  }

  // ─── Create / Edit / View ──────────────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      subcontractorId: null, warehouseId: null, processType: '',
      orderDate: this.todayIso(), expectedReturnDate: null, chargeAmount: 0, notes: ''
    });
    this.addLine();
    this.dialogVisible = true;
  }

  openEdit(order: SubcontractOrderListItemDto): void {
    this.editingId = order.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.dialogVisible = true;

    this.service.getById(order.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const o = res.data;
          this.dialogMode = o.status === 'Draft' ? 'edit' : 'view';
          this.form.patchValue({
            subcontractorId: o.subcontractorId, warehouseId: o.warehouseId, processType: o.processType,
            orderDate: o.orderDate, expectedReturnDate: o.expectedReturnDate ?? null,
            chargeAmount: o.chargeAmount, notes: o.notes ?? ''
          });
          o.lines.forEach(l => this.lines.push(this.newLine(
            l.rawMaterialId ? 'R:' + l.rawMaterialId : 'P:' + l.productId, l.issuedQuantity, l.lineNotes ?? '')));
          if (this.dialogMode === 'view') this.form.disable();
          this.cdr.detectChanges();
        }
      })
    });
  }

  private mapLines(): any[] {
    return (this.form.getRawValue().lines as any[]).map(l => {
      const [type, id] = (l.itemKey as string).split(':');
      return {
        rawMaterialId: type === 'R' ? Number(id) : null,
        productId: type === 'P' ? Number(id) : null,
        issuedQuantity: Number(l.issuedQuantity) || 0,
        lineNotes: (l.lineNotes as string)?.trim() || null
      };
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving || this.dialogMode === 'view') return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();
    const body = {
      subcontractorId: v.subcontractorId, orderDate: v.orderDate,
      expectedReturnDate: v.expectedReturnDate || null, processType: (v.processType as string).trim(),
      warehouseId: v.warehouseId, chargeAmount: Number(v.chargeAmount) || 0,
      notes: (v.notes as string)?.trim() || null, lines: this.mapLines()
    };

    const obs = this.dialogMode === 'create'
      ? this.service.create(body)
      : this.service.update(this.editingId!, body);
    obs.subscribe({ next: (res) => this.handleSave(res), error: (err) => this.handleError(err) });
  }

  private handleSave(res: { success: boolean; message?: string | null }): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      if (res.success) { this.dialogVisible = false; this.load(); }
      else this.dialogError = res.message || 'Save failed.';
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

  // ─── Row actions: issue / cancel ───────────────────────────────────────────

  issue(order: SubcontractOrderListItemDto): void { this.runRowAction(order, this.service.issue.bind(this.service)); }
  cancel(order: SubcontractOrderListItemDto): void { this.runRowAction(order, this.service.cancel.bind(this.service)); }

  private runRowAction(order: SubcontractOrderListItemDto, fn: (id: number) => any): void {
    if (this.rowActionId) return;
    this.rowActionId = order.id;
    this.actionError = '';
    this.cdr.detectChanges();
    fn(order.id).subscribe({
      next: (res: { success: boolean; message?: string | null }) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) this.load(); else this.actionError = res.message || 'Action failed.';
        this.cdr.detectChanges();
      }),
      error: (err: any) => this.zone.run(() => {
        this.rowActionId = null;
        this.actionError = err?.error?.message || 'Action failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ─── Receive ───────────────────────────────────────────────────────────────

  openReceive(order: SubcontractOrderListItemDto): void {
    this.receiveError = '';
    this.service.getById(order.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          this.receiveOrder = res.data;
          this.receiveForm = this.fb.group({
            lines: this.fb.array(res.data.lines.map(l => this.fb.group({
              lineId: [l.id],
              itemLabel: [`${l.itemCode} — ${l.itemName}`],
              issuedQuantity: [l.issuedQuantity],
              receivedQuantity: [l.issuedQuantity, [Validators.required, Validators.min(0)]]
            })))
          });
          this.receiveVisible = true;
          this.cdr.detectChanges();
        }
      })
    });
  }

  get receiveLines(): FormArray { return this.receiveForm.get('lines') as FormArray; }

  doReceive(): void {
    if (!this.receiveOrder || this.receiveForm.invalid || this.receiveSaving) return;
    this.receiveSaving = true;
    this.receiveError = '';
    this.cdr.detectChanges();
    const lines = (this.receiveForm.getRawValue().lines as any[]).map(l => ({
      lineId: l.lineId, receivedQuantity: Number(l.receivedQuantity) || 0
    }));
    this.service.receive(this.receiveOrder.id, lines).subscribe({
      next: (res) => this.zone.run(() => {
        this.receiveSaving = false;
        if (res.success) { this.receiveVisible = false; this.receiveOrder = null; this.load(); }
        else this.receiveError = res.message || 'Receive failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.receiveSaving = false;
        this.receiveError = err?.error?.message || 'Receive failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ─── Delete ────────────────────────────────────────────────────────────────

  confirmDelete(order: SubcontractOrderListItemDto): void {
    this.deletingOrder = order;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingOrder || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();
    this.service.delete(this.deletingOrder.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) { this.deleteDialogVisible = false; this.deletingOrder = null; this.load(); }
        else this.deleteError = res.message || 'Delete failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.deleting = false;
        this.deleteError = err?.error?.message || 'Delete failed.';
        this.cdr.detectChanges();
      })
    });
  }

  canEdit(o: SubcontractOrderListItemDto): boolean { return o.status === 'Draft'; }
  canIssue(o: SubcontractOrderListItemDto): boolean { return o.status === 'Draft'; }
  canReceive(o: SubcontractOrderListItemDto): boolean { return o.status === 'Issued'; }
  canCancel(o: SubcontractOrderListItemDto): boolean { return o.status === 'Draft'; }
  canDelete(o: SubcontractOrderListItemDto): boolean { return o.status === 'Draft' || o.status === 'Cancelled'; }
}
