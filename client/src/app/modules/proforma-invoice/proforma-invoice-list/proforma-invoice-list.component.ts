import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ProformaInvoiceService } from '../../../services/proforma-invoice.service';
import { CustomerService } from '../../../services/customer.service';
import { ProductService } from '../../../services/product.service';
import { CurrencyService } from '../../../services/currency.service';
import { SalesOrderService } from '../../../services/sales-order.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  ProformaInvoiceDto, PFM_STATUSES, ProformaInvoiceLineDto
} from '../../../models/proforma-invoice.models';
import { CustomerListItemDto } from '../../../models/customer.models';
import { ProductListItemDto } from '../../../models/product.models';
import { CurrencyDto } from '../../../models/master-data.models';

@Component({
  selector: 'app-proforma-invoice-list',
  standalone: false,
  templateUrl: './proforma-invoice-list.component.html',
  styleUrl: './proforma-invoice-list.component.scss'
})
export class ProformaInvoiceListComponent implements OnInit {

  proformas: ProformaInvoiceDto[] = [];
  loading = false;
  totalCount = 0;
  actionError = '';
  actionMessage = '';
  rowActionId: number | null = null;

  readonly statuses = PFM_STATUSES;
  customers: CustomerListItemDto[] = [];
  products: ProductListItemDto[] = [];
  currencies: CurrencyDto[] = [];

  filterStatus: string | null = null;
  filterCustomerId: number | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Create/Edit/View dialog
  dialogVisible = false;
  dialogSaving = false;
  dialogError = '';
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  editing: ProformaInvoiceDto | null = null;
  form!: FormGroup;
  viewLines: ProformaInvoiceLineDto[] = [];

  // Confirm dialogs
  cancelVisible = false; cancelTarget: ProformaInvoiceDto | null = null;
  cancelling = false; cancelError = '';

  deleteVisible = false; deleteTarget: ProformaInvoiceDto | null = null;
  deleting = false; deleteError = '';

  // Convert dialog
  convertVisible = false;
  convertTarget: ProformaInvoiceDto | null = null;
  convertSalesOrders: any[] = [];
  convertForm!: FormGroup;
  converting = false;
  convertError = '';

  constructor(
    private service: ProformaInvoiceService,
    private customerService: CustomerService,
    private productService: ProductService,
    private currencyService: CurrencyService,
    private salesOrderService: SalesOrderService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      customerId: [null, Validators.required],
      currencyId: [null, Validators.required],
      exchangeRate: [1, [Validators.required, Validators.min(0.000001)]],
      issueDate: [this.todayIso(), Validators.required],
      validUntil: [this.addDaysIso(30), Validators.required],
      vatRate: [0.15, [Validators.required, Validators.min(0), Validators.max(1)]],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
    this.convertForm = this.fb.group({
      salesOrderId: [null, Validators.required],
      invoiceDate: [this.todayIso(), Validators.required],
      dueDate: [null]
    });
    this.loadCustomers();
    this.loadProducts();
    this.loadCurrencies();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().substring(0, 10); }
  private addDaysIso(d: number): string {
    const t = new Date(); t.setDate(t.getDate() + d);
    return t.toISOString().substring(0, 10);
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }
  newLineGroup(productId: number | null = null, quantity = 1, unitPrice = 0, lineNotes = ''): FormGroup {
    return this.fb.group({
      productId: [productId, Validators.required],
      quantity: [quantity, [Validators.required, Validators.min(0.0001)]],
      unitPrice: [unitPrice, [Validators.required, Validators.min(0)]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  formatCurrency(amount: number, code = 'BDT'): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: code, maximumFractionDigits: 2 }).format(amount || 0);
  }

  statusBadgeClass(s: string): string {
    switch (s) {
      case 'Draft': return 'draft';
      case 'Sent': return 'sent';
      case 'Accepted': return 'accepted';
      case 'Expired': return 'expired';
      case 'Cancelled': return 'cancelled';
      case 'Converted': return 'converted';
      default: return '';
    }
  }

  private loadCustomers(): void {
    this.customerService.getAll({ page: 1, pageSize: 1000, search: '' }).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.customers = res.data.items;
        this.cdr.detectChanges();
      })
    });
  }

  private loadProducts(): void {
    this.productService.getAll({ page: 1, pageSize: 1000, search: '' }).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.products = res.data.items;
        this.cdr.detectChanges();
      })
    });
  }

  private loadCurrencies(): void {
    this.currencyService.getAll().subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          this.currencies = res.data;
          const bdt = this.currencies.find(c => c.code === 'BDT');
          if (bdt && !this.form.get('currencyId')?.value) {
            this.form.patchValue({ currencyId: bdt.id, exchangeRate: 1 });
          }
        }
        this.cdr.detectChanges();
      })
    });
  }

  onCurrencyChange(ev: any): void {
    const id = ev?.value;
    const c = this.currencies.find(x => x.id === id);
    if (c) this.form.patchValue({ exchangeRate: c.exchangeRateToBase });
  }

  load(): void {
    this.loading = true;
    this.service.getAll(this.parameters, this.filterStatus ?? undefined, this.filterCustomerId ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.proformas = res.data.items;
          this.totalCount = res.data.totalCount;
        }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
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

  productName(id: number): string {
    const p = this.products.find(x => x.id === id);
    return p ? `${p.name} (${p.code})` : '';
  }

  productUnit(id: number): string {
    const p = this.products.find(x => x.id === id);
    return p?.unitOfMeasureCode ?? '';
  }

  lineTotal(lineCtrl: any): number {
    const q = Number(lineCtrl.get('quantity')?.value || 0);
    const u = Number(lineCtrl.get('unitPrice')?.value || 0);
    return q * u;
  }

  get subtotal(): number {
    return this.lines.controls.reduce((sum, l) => sum + this.lineTotal(l), 0);
  }

  get vatAmount(): number {
    return Math.round(this.subtotal * (Number(this.form.get('vatRate')?.value) || 0) * 10000) / 10000;
  }

  get grandTotal(): number {
    return this.subtotal + this.vatAmount;
  }

  // ── Open dialogs ─────────────────────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editing = null;
    this.dialogError = '';
    const bdt = this.currencies.find(c => c.code === 'BDT');
    this.form.reset({
      customerId: null,
      currencyId: bdt?.id ?? null,
      exchangeRate: 1,
      issueDate: this.todayIso(),
      validUntil: this.addDaysIso(30),
      vatRate: 0.15,
      notes: ''
    });
    this.lines.clear();
    this.lines.push(this.newLineGroup());
    this.form.enable();
    this.dialogVisible = true;
  }

  openEdit(p: ProformaInvoiceDto): void {
    this.dialogMode = p.status === 'Draft' ? 'edit' : 'view';
    this.editing = p;
    this.dialogError = '';
    this.service.getById(p.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const full = res.data;
          this.form.patchValue({
            customerId: full.customerId,
            currencyId: full.currencyId,
            exchangeRate: full.exchangeRate,
            issueDate: full.issueDate,
            validUntil: full.validUntil,
            vatRate: full.vatRate,
            notes: full.notes ?? ''
          });
          this.lines.clear();
          full.lines.forEach(l => this.lines.push(this.newLineGroup(
            l.productId, l.quantity, l.unitPrice, l.lineNotes ?? ''
          )));
          this.viewLines = full.lines;
          if (this.dialogMode === 'view') this.form.disable(); else this.form.enable();
          this.dialogVisible = true;
        }
        this.cdr.detectChanges();
      })
    });
  }

  addLine(): void { this.lines.push(this.newLineGroup()); }
  removeLine(i: number): void { this.lines.removeAt(i); }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const linesPayload = (v.lines as any[]).map(l => ({
      productId: Number(l.productId),
      quantity: Number(l.quantity),
      unitPrice: Number(l.unitPrice),
      lineNotes: (l.lineNotes as string)?.trim() || null
    }));
    const obs: any = this.dialogMode === 'create'
      ? this.service.create({
          customerId: Number(v.customerId),
          salesOrderId: null,
          currencyId: Number(v.currencyId),
          exchangeRate: Number(v.exchangeRate),
          issueDate: v.issueDate,
          validUntil: v.validUntil,
          vatRate: Number(v.vatRate),
          notes: (v.notes as string)?.trim() || null,
          lines: linesPayload
        })
      : this.service.update(this.editing!.id, {
          issueDate: v.issueDate,
          validUntil: v.validUntil,
          vatRate: Number(v.vatRate),
          notes: (v.notes as string)?.trim() || null,
          lines: linesPayload
        });
    obs.subscribe({
      next: (res: any) => this.zone.run(() => {
        this.dialogSaving = false;
        if (res.success) {
          this.dialogVisible = false;
          this.actionMessage = res.message || 'Saved.';
          this.load();
        } else {
          this.dialogError = res.message || 'Save failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err: any) => this.zone.run(() => {
        this.dialogSaving = false;
        this.dialogError = err?.error?.message || 'Save failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── State actions ────────────────────────────────────────────────────────

  send(p: ProformaInvoiceDto): void { this.actAction(p, 'send'); }
  accept(p: ProformaInvoiceDto): void { this.actAction(p, 'accept'); }
  expire(p: ProformaInvoiceDto): void { this.actAction(p, 'expire'); }

  private actAction(p: ProformaInvoiceDto, action: 'send' | 'accept' | 'expire'): void {
    if (this.rowActionId) return;
    this.rowActionId = p.id;
    this.actionError = '';
    this.cdr.detectChanges();
    const obs = action === 'send' ? this.service.send(p.id)
              : action === 'accept' ? this.service.accept(p.id)
              : this.service.expire(p.id);
    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) { this.actionMessage = res.message || 'Done.'; this.load(); }
        else this.actionError = res.message || 'Action failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.rowActionId = null;
        this.actionError = err?.error?.message || 'Action failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Cancel ───────────────────────────────────────────────────────────────

  openCancel(p: ProformaInvoiceDto): void {
    this.cancelTarget = p; this.cancelError = ''; this.cancelVisible = true;
  }
  doCancel(): void {
    if (!this.cancelTarget || this.cancelling) return;
    this.cancelling = true; this.cancelError = ''; this.cdr.detectChanges();
    this.service.cancel(this.cancelTarget.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.cancelling = false;
        if (res.success) { this.cancelVisible = false; this.actionMessage = res.message || 'Cancelled.'; this.load(); }
        else this.cancelError = res.message || 'Cancel failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.cancelling = false;
        this.cancelError = err?.error?.message || 'Cancel failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Delete ───────────────────────────────────────────────────────────────

  openDelete(p: ProformaInvoiceDto): void {
    this.deleteTarget = p; this.deleteError = ''; this.deleteVisible = true;
  }
  doDelete(): void {
    if (!this.deleteTarget || this.deleting) return;
    this.deleting = true; this.deleteError = ''; this.cdr.detectChanges();
    this.service.delete(this.deleteTarget.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) { this.deleteVisible = false; this.actionMessage = res.message || 'Deleted.'; this.load(); }
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

  // ── Convert to CustomerInvoice ───────────────────────────────────────────

  openConvert(p: ProformaInvoiceDto): void {
    this.convertTarget = p;
    this.convertError = '';
    this.convertForm.reset({
      salesOrderId: null,
      invoiceDate: this.todayIso(),
      dueDate: null
    });
    // Load SOs for this customer (Confirmed/Dispatched/Delivered)
    this.salesOrderService.getAll({ page: 1, pageSize: 500, search: '' }, p.customerId).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          this.convertSalesOrders = res.data.items.filter((so: any) =>
            so.status === 'Confirmed' || so.status === 'PartiallyDispatched' ||
            so.status === 'Dispatched' || so.status === 'Delivered');
        }
        this.convertVisible = true;
        this.cdr.detectChanges();
      })
    });
  }

  doConvert(): void {
    if (!this.convertTarget || this.convertForm.invalid || this.converting) return;
    this.converting = true;
    this.convertError = '';
    this.cdr.detectChanges();
    const v = this.convertForm.getRawValue();
    this.service.convert(this.convertTarget.id, {
      salesOrderId: Number(v.salesOrderId),
      invoiceDate: v.invoiceDate,
      dueDate: v.dueDate || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.converting = false;
        if (res.success) {
          this.convertVisible = false;
          this.actionMessage = res.message || 'Converted to customer invoice.';
          this.load();
        } else {
          this.convertError = res.message || 'Convert failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.converting = false;
        this.convertError = err?.error?.message || 'Convert failed.';
        this.cdr.detectChanges();
      })
    });
  }
}
