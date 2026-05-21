import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PurchaseOrderService } from '../../../services/purchase-order.service';
import { SupplierService } from '../../../services/supplier.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { PO_STATUSES, PurchaseOrderListItemDto } from '../../../models/purchase-order.models';
import { SupplierListItemDto } from '../../../models/supplier.models';
import { WarehouseDto, CurrencyDto } from '../../../models/master-data.models';
import { RawMaterialListItemDto } from '../../../models/raw-material.models';
import { CurrencyService } from '../../../services/currency.service';

@Component({
  selector: 'app-purchase-order-list',
  standalone: false,
  templateUrl: './purchase-order-list.component.html',
  styleUrl: './purchase-order-list.component.scss'
})
export class PurchaseOrderListComponent implements OnInit {

  pos: PurchaseOrderListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterSupplierId: number | null = null;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Dropdown sources
  readonly statuses = PO_STATUSES;
  suppliers: SupplierListItemDto[] = [];
  warehouses: WarehouseDto[] = [];
  rawMaterials: RawMaterialListItemDto[] = [];
  currencies: CurrencyDto[] = [];

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingPo: PurchaseOrderListItemDto | null = null;
  deleting = false;
  deleteError = '';

  // Row action (approve / send / cancel) in-flight id
  rowActionId: number | null = null;

  constructor(
    private poService: PurchaseOrderService,
    private supplierService: SupplierService,
    private warehouseService: WarehouseService,
    private rawMaterialService: RawMaterialService,
    private currencyService: CurrencyService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadDropdowns();
    this.load();
  }

  // ─── Form ────────────────────────────────────────────────────────────────

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private buildForm(): void {
    this.form = this.fb.group({
      supplierId: [null as number | null, Validators.required],
      orderDate: [this.todayIso(), Validators.required],
      expectedDeliveryDate: [null as string | null],
      deliveryWarehouseId: [null as number | null],
      currencyId: [null as number | null, Validators.required],
      exchangeRate: [1, [Validators.required, Validators.min(0.000001)]],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  private newLine(
    rawMaterialId: number | null = null,
    quantity = 1,
    unitPrice = 0,
    lineNotes = ''
  ): FormGroup {
    return this.fb.group({
      rawMaterialId: [rawMaterialId, Validators.required],
      quantity: [quantity, [Validators.required, Validators.min(0.0001)]],
      unitPrice: [unitPrice, [Validators.required, Validators.min(0)]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  addLine(): void {
    this.lines.push(this.newLine());
  }

  removeLine(index: number): void {
    this.lines.removeAt(index);
  }

  onLineRmChange(line: AbstractControl, event: any): void {
    const rm = this.rawMaterialById(event?.value);
    if (rm) line.get('unitPrice')?.setValue(rm.standardCost);
  }

  // ─── Line computations ───────────────────────────────────────────────────

  rawMaterialById(id: number | null | undefined): RawMaterialListItemDto | undefined {
    return id ? this.rawMaterials.find(r => r.id === id) : undefined;
  }

  lineUomCode(line: AbstractControl): string {
    return this.rawMaterialById(line.get('rawMaterialId')?.value)?.unitOfMeasureCode ?? '—';
  }

  lineTotal(line: AbstractControl): number {
    const qty = Number(line.get('quantity')?.value) || 0;
    const price = Number(line.get('unitPrice')?.value) || 0;
    return qty * price;
  }

  totalAmount(): number {
    return this.lines.controls.reduce((sum, l) => sum + this.lineTotal(l), 0);
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency',
      currency: 'BDT',
      maximumFractionDigits: 2
    }).format(amount || 0);
  }

  /** Format an amount in a given ISO currency code (falls back to plain + code). */
  formatMoney(amount: number, code: string | null | undefined): string {
    const c = code || 'BDT';
    try {
      return new Intl.NumberFormat('en-US', {
        style: 'currency', currency: c, maximumFractionDigits: 2
      }).format(amount || 0);
    } catch {
      return `${(amount || 0).toLocaleString('en-US', { maximumFractionDigits: 2 })} ${c}`;
    }
  }

  get currentCurrencyCode(): string {
    return this.currencyCodeById(this.form?.get('currencyId')?.value) || 'BDT';
  }

  formatDate(d: string | null): string {
    return d ? d : '—';
  }

  // ─── Data loading ────────────────────────────────────────────────────────

  private loadDropdowns(): void {
    this.supplierService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.suppliers = res.data.items;
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
    this.rawMaterialService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.rawMaterials = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
    this.currencyService.getAll(false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.currencies = res.data;
          // default a brand-new form to the base currency once currencies arrive
          if (this.dialogVisible && this.dialogMode === 'create' && !this.form.get('currencyId')?.value) {
            this.form.patchValue({ currencyId: this.baseCurrencyId(), exchangeRate: 1 });
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  private baseCurrencyId(): number | null {
    return this.currencies.find(c => c.isBaseCurrency)?.id ?? this.currencies[0]?.id ?? null;
  }

  currencyCodeById(id: number | null | undefined): string {
    return id ? (this.currencies.find(c => c.id === id)?.code ?? '') : '';
  }

  onCurrencyChange(event: any): void {
    const cur = this.currencies.find(c => c.id === event?.value);
    if (cur) this.form.get('exchangeRate')?.setValue(cur.exchangeRateToBase);
  }

  load(): void {
    this.loading = true;
    this.poService.getAll(
      this.parameters,
      this.filterSupplierId ?? undefined,
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

  // ─── Create / Edit / View dialog ─────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      supplierId: this.filterSupplierId ?? null,
      orderDate: this.todayIso(),
      expectedDeliveryDate: null,
      deliveryWarehouseId: null,
      currencyId: this.baseCurrencyId(),
      exchangeRate: 1,
      notes: ''
    });
    this.addLine();
    this.dialogVisible = true;
  }

  openEdit(po: PurchaseOrderListItemDto): void {
    this.editingId = po.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.dialogVisible = true;

    this.poService.getById(po.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const p = res.data;
            this.dialogMode = p.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              supplierId: p.supplierId,
              orderDate: p.orderDate,
              expectedDeliveryDate: p.expectedDeliveryDate ?? null,
              deliveryWarehouseId: p.deliveryWarehouseId ?? null,
              currencyId: p.currencyId,
              exchangeRate: p.exchangeRate,
              notes: p.notes ?? ''
            });
            p.lines.forEach(l => this.lines.push(
              this.newLine(l.rawMaterialId, l.quantity, l.unitPrice, l.lineNotes ?? '')
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
    const lines = (v.lines as any[]).map(l => ({
      rawMaterialId: l.rawMaterialId,
      quantity: Number(l.quantity) || 0,
      unitPrice: Number(l.unitPrice) || 0,
      lineNotes: (l.lineNotes as string)?.trim() || null
    }));

    const baseFields = {
      supplierId: v.supplierId,
      orderDate: v.orderDate,
      expectedDeliveryDate: v.expectedDeliveryDate || null,
      deliveryWarehouseId: v.deliveryWarehouseId ?? null,
      currencyId: v.currencyId,
      exchangeRate: Number(v.exchangeRate) || 1,
      notes: (v.notes as string)?.trim() || null,
      lines
    };

    if (this.dialogMode === 'create') {
      this.poService.create(baseFields).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.poService.update(this.editingId, baseFields).subscribe({
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

  submitForApproval(po: PurchaseOrderListItemDto): void {
    this.runRowAction(po, this.poService.submitForApproval.bind(this.poService));
  }

  send(po: PurchaseOrderListItemDto): void {
    this.runRowAction(po, this.poService.send.bind(this.poService));
  }

  cancel(po: PurchaseOrderListItemDto): void {
    this.runRowAction(po, this.poService.cancel.bind(this.poService));
  }

  close(po: PurchaseOrderListItemDto): void {
    this.runRowAction(po, this.poService.close.bind(this.poService));
  }

  private runRowAction(
    po: PurchaseOrderListItemDto,
    fn: (id: number) => any
  ): void {
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

  canCancel(po: PurchaseOrderListItemDto): boolean {
    return po.status === 'Draft' || po.status === 'Approved' || po.status === 'Sent';
  }

  canDelete(po: PurchaseOrderListItemDto): boolean {
    return po.status === 'Draft' || po.status === 'Cancelled';
  }

  // ─── Delete ──────────────────────────────────────────────────────────────

  confirmDelete(po: PurchaseOrderListItemDto): void {
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
}
