import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SupplierInvoiceService } from '../../../services/supplier-invoice.service';
import { PurchaseOrderService } from '../../../services/purchase-order.service';
import { SupplierService } from '../../../services/supplier.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  SINV_STATUSES,
  SupplierInvoiceListItemDto
} from '../../../models/supplier-invoice.models';
import { PurchaseOrderDto } from '../../../models/purchase-order.models';
import { SupplierListItemDto } from '../../../models/supplier.models';

interface InvoiceablePoOption {
  id: number;
  code: string;
  supplierName: string;
  status: string;
  displayLabel: string;
}

@Component({
  selector: 'app-supplier-invoice-list',
  standalone: false,
  templateUrl: './supplier-invoice-list.component.html',
  styleUrl: './supplier-invoice-list.component.scss'
})
export class SupplierInvoiceListComponent implements OnInit {

  invoices: SupplierInvoiceListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterSupplierId: number | null = null;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = SINV_STATUSES;
  suppliers: SupplierListItemDto[] = [];
  invoiceablePos: InvoiceablePoOption[] = [];
  selectedPo: PurchaseOrderDto | null = null;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deletingInv: SupplierInvoiceListItemDto | null = null;
  deleting = false;
  deleteError = '';

  rowActionId: number | null = null;

  constructor(
    private invService: SupplierInvoiceService,
    private poService: PurchaseOrderService,
    private supplierService: SupplierService,
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
      purchaseOrderId: [null as number | null, Validators.required],
      supplierInvoiceNumber: ['', Validators.maxLength(100)],
      vatRatePercent: [15, [Validators.required, Validators.min(0), Validators.max(100)]],
      invoiceDate: [this.todayIso(), Validators.required],
      dueDate: [null as string | null],
      notes: ['', Validators.maxLength(2000)],
      isOpening: [false],   // Phase A1
      lines: this.fb.array([])
    });
  }

  get vatRateDecimal(): number {
    return (Number(this.form.get('vatRatePercent')?.value) || 0) / 100;
  }

  subtotalAmount(): number {
    return this.lines.controls.reduce((sum, l) => sum + this.lineTotal(l), 0);
  }

  vatAmount(): number {
    return Math.round(this.subtotalAmount() * this.vatRateDecimal * 10000) / 10000;
  }

  grandTotalAmount(): number {
    return this.subtotalAmount() + this.vatAmount();
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  private loadDropdowns(): void {
    this.supplierService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.suppliers = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
    this.poService.getAll({ page: 1, pageSize: 500, search: '' }).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.invoiceablePos = res.data.items
              .filter(p =>
                p.status === 'Approved' || p.status === 'Sent' ||
                p.status === 'PartiallyReceived' || p.status === 'Received' ||
                p.status === 'Closed')
              .map(p => ({
                id: p.id,
                code: p.code,
                supplierName: p.supplierName,
                status: p.status,
                displayLabel: `${p.code} — ${p.supplierName}`
              }));
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.invService.getAll(
      this.parameters,
      this.filterSupplierId ?? undefined,
      undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.invoices = res.data.items;
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

  private newLine(rawMaterialId: number | null, rawMaterialDisplay: string, uomCode: string,
                  quantity: number, unitPrice: number, lineNotes = ''): FormGroup {
    return this.fb.group({
      rawMaterialId: [rawMaterialId, Validators.required],
      rawMaterialDisplay: [rawMaterialDisplay],
      uomCode: [uomCode],
      quantity: [quantity, [Validators.required, Validators.min(0.0001)]],
      unitPrice: [unitPrice, [Validators.required, Validators.min(0)]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  private buildLinesFromPo(po: PurchaseOrderDto): void {
    this.lines.clear();
    for (const poLine of po.lines) {
      this.lines.push(this.newLine(
        poLine.rawMaterialId,
        `${poLine.rawMaterialCode} — ${poLine.rawMaterialName}`,
        poLine.unitOfMeasureCode,
        poLine.quantity,
        poLine.unitPrice,
        ''
      ));
    }
  }

  onPoChange(event: any): void {
    const poId = event?.value;
    if (!poId) {
      this.lines.clear();
      this.selectedPo = null;
      return;
    }
    this.poService.getById(poId).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.selectedPo = res.data;
            this.buildLinesFromPo(res.data);
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
    this.selectedPo = null;
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      purchaseOrderId: null,
      supplierInvoiceNumber: '',
      vatRatePercent: 15,
      invoiceDate: this.todayIso(),
      dueDate: null,
      notes: '',
      isOpening: false
    });
    this.dialogVisible = true;
  }

  openEdit(inv: SupplierInvoiceListItemDto): void {
    this.editingId = inv.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.selectedPo = null;
    this.dialogVisible = true;

    this.invService.getById(inv.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const s = res.data;
            this.dialogMode = s.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              purchaseOrderId: s.purchaseOrderId,
              supplierInvoiceNumber: s.supplierInvoiceNumber ?? '',
              vatRatePercent: Math.round((s.vatRate || 0) * 10000) / 100,
              invoiceDate: s.invoiceDate,
              dueDate: s.dueDate,
              notes: s.notes ?? ''
            });
            for (const l of s.lines) {
              this.lines.push(this.newLine(
                l.rawMaterialId,
                `${l.rawMaterialCode} — ${l.rawMaterialName}`,
                l.unitOfMeasureCode,
                l.quantity,
                l.unitPrice,
                l.lineNotes ?? ''
              ));
            }
            this.form.get('purchaseOrderId')?.disable();
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

    if (this.dialogMode === 'create') {
      this.invService.create({
        purchaseOrderId: v.purchaseOrderId,
        supplierInvoiceNumber: (v.supplierInvoiceNumber as string)?.trim() || null,
        vatRate: this.vatRateDecimal,
        invoiceDate: v.invoiceDate,
        dueDate: v.dueDate || null,
        notes: (v.notes as string)?.trim() || null,
        lines,
        isOpening: !!v.isOpening   // Phase A1
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.invService.update(this.editingId, {
        supplierInvoiceNumber: (v.supplierInvoiceNumber as string)?.trim() || null,
        vatRate: this.vatRateDecimal,
        invoiceDate: v.invoiceDate,
        dueDate: v.dueDate || v.invoiceDate,
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

  approve(inv: SupplierInvoiceListItemDto): void {
    this.runRowAction(inv, this.invService.approve.bind(this.invService));
  }

  cancel(inv: SupplierInvoiceListItemDto): void {
    this.runRowAction(inv, this.invService.cancel.bind(this.invService));
  }

  private runRowAction(inv: SupplierInvoiceListItemDto, fn: (id: number) => any): void {
    if (this.rowActionId) return;
    this.rowActionId = inv.id;
    this.actionError = '';
    this.cdr.detectChanges();
    fn(inv.id).subscribe({
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

  canCancel(inv: SupplierInvoiceListItemDto): boolean {
    return (inv.status === 'Draft' || inv.status === 'Approved') && inv.amountPaid === 0;
  }

  canDelete(inv: SupplierInvoiceListItemDto): boolean {
    return inv.status === 'Draft' || inv.status === 'Cancelled';
  }

  confirmDelete(inv: SupplierInvoiceListItemDto): void {
    this.deletingInv = inv;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingInv || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.invService.delete(this.deletingInv.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingInv = null;
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

  lineTotal(line: AbstractControl): number {
    const qty = Number(line.get('quantity')?.value) || 0;
    const price = Number(line.get('unitPrice')?.value) || 0;
    return qty * price;
  }

  meta(line: AbstractControl) {
    return line.getRawValue();
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency', currency: 'BDT', maximumFractionDigits: 2
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
}
