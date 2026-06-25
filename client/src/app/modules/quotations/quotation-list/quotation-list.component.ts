import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { QuotationService } from '../../../services/quotation.service';
import { CustomerService } from '../../../services/customer.service';
import { ProductService } from '../../../services/product.service';
import { CurrencyService } from '../../../services/currency.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { QUOTATION_STATUSES, QuotationDto, QuotationListItemDto } from '../../../models/quotation.models';
import { CurrencyDto } from '../../../models/master-data.models';

@Component({
  selector: 'app-quotation-list',
  standalone: false,
  templateUrl: './quotation-list.component.html',
  styleUrl: './quotation-list.component.scss'
})
export class QuotationListComponent implements OnInit {
  quotations: QuotationListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;
  actionError = '';
  actionMessage = '';
  rowActionId: number | null = null;

  // Email dialog
  emailDlgOpen = false;
  emailSourceId = 0;

  openEmail(row: { id: number }): void {
    this.emailSourceId = row.id;
    this.emailDlgOpen = true;
  }

  onEmailSent(ev: { sourceCode: string }): void {
    this.actionMessage = `Email sent for ${ev.sourceCode}.`;
    this.cdr.detectChanges();
  }

  readonly statuses = QUOTATION_STATUSES;
  customers: any[] = [];
  products: any[] = [];
  currencies: CurrencyDto[] = [];

  canCreate = false;
  canSend = false;
  canConvert = false;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  loaded: QuotationDto | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deleting: QuotationListItemDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(
    private svc: QuotationService,
    private customerService: CustomerService,
    private productService: ProductService,
    private currencyService: CurrencyService,
    private auth: AuthService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canCreate = this.auth.hasPermission('Quotations.Create');
    this.canSend = this.auth.hasPermission('Quotations.Send');
    this.canConvert = this.auth.hasPermission('Quotations.Convert');
    this.form = this.fb.group({
      customerId: [null as number | null, Validators.required],
      quotationDate: [this.todayIso(), Validators.required],
      validUntil: [null as string | null],
      currencyId: [null as number | null, Validators.required],
      exchangeRate: [1, [Validators.required, Validators.min(0.000001)]],
      customerReference: ['', Validators.maxLength(100)],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
    this.loadDropdowns();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }
  get lines(): FormArray { return this.form.get('lines') as FormArray; }

  private newLine(): FormGroup {
    return this.fb.group({
      productId: [null as number | null, Validators.required],
      description: [''],
      quantity: [1, [Validators.required, Validators.min(0.0001)]],
      materialCost: [0], laborCost: [0], machineCost: [0], overheadCost: [0],
      wastagePercent: [0], marginPercent: [0]
    });
  }
  addLine(): void { this.lines.push(this.newLine()); }
  removeLine(i: number): void { this.lines.removeAt(i); }

  // ── Live costing (mirror of backend formula) ──
  lineUnitCost(c: AbstractControl): number {
    const v = c.getRawValue();
    const base = (+v.materialCost || 0) + (+v.laborCost || 0) + (+v.machineCost || 0) + (+v.overheadCost || 0);
    return base * (1 + (+v.wastagePercent || 0) / 100);
  }
  lineUnitPrice(c: AbstractControl): number { return this.lineUnitCost(c) * (1 + (+c.getRawValue().marginPercent || 0) / 100); }
  lineTotal(c: AbstractControl): number { return this.lineUnitPrice(c) * (+c.getRawValue().quantity || 0); }
  get grandTotal(): number { return this.lines.controls.reduce((s, c) => s + this.lineTotal(c), 0); }

  private loadDropdowns(): void {
    this.customerService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.customers = res.data.items; this.cdr.detectChanges(); })
    });
    this.productService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.products = res.data.items; this.cdr.detectChanges(); })
    });
    this.currencyService.getAll(false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.currencies = res.data; this.cdr.detectChanges(); })
    });
  }

  onCurrencyChange(ev: any): void {
    const c = this.currencies.find(x => x.id === ev?.value);
    if (c) this.form.get('exchangeRate')?.setValue(c.exchangeRateToBase);
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters, undefined, this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.quotations = res.data.items; this.totalCount = res.data.totalCount; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearch(v: string): void { if (this.searchTimer) clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => { this.parameters.search = v; this.parameters.page = 1; this.load(); }, 350); }
  onPage(e: any): void { this.parameters.page = Math.floor(e.first / e.rows) + 1; this.parameters.pageSize = e.rows; this.load(); }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = ''; this.loaded = null;
    this.lines.clear();
    const baseCur = this.currencies.find(c => c.isBaseCurrency) ?? this.currencies[0];
    this.form.reset({ customerId: null, quotationDate: this.todayIso(), validUntil: null, currencyId: baseCur?.id ?? null, exchangeRate: baseCur?.exchangeRateToBase ?? 1, customerReference: '', notes: '' });
    this.form.enable();
    this.addLine();
    this.dialogVisible = true;
  }

  open(q: QuotationListItemDto): void {
    this.editingId = q.id; this.dialogError = ''; this.dialogVisible = true; this.lines.clear(); this.form.enable();
    this.svc.getById(q.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const x = res.data; this.loaded = x;
          this.dialogMode = x.status === 'Draft' ? 'edit' : 'view';
          this.form.patchValue({
            customerId: x.customerId, quotationDate: x.quotationDate, validUntil: x.validUntil,
            currencyId: x.currencyId, exchangeRate: x.exchangeRate, customerReference: x.customerReference ?? '', notes: x.notes ?? ''
          });
          for (const l of x.lines) {
            this.lines.push(this.fb.group({
              productId: [l.productId, Validators.required], description: [l.description ?? ''],
              quantity: [l.quantity, [Validators.required, Validators.min(0.0001)]],
              materialCost: [l.materialCost], laborCost: [l.laborCost], machineCost: [l.machineCost], overheadCost: [l.overheadCost],
              wastagePercent: [l.wastagePercent], marginPercent: [l.marginPercent]
            }));
          }
          if (this.dialogMode === 'view') this.form.disable();
          this.cdr.detectChanges();
        }
      })
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving || this.dialogMode === 'view') return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const payload = {
      customerId: v.customerId, quotationDate: v.quotationDate, validUntil: v.validUntil || null,
      currencyId: v.currencyId, exchangeRate: Number(v.exchangeRate) || 1,
      customerReference: (v.customerReference as string)?.trim() || null, notes: (v.notes as string)?.trim() || null,
      lines: (v.lines as any[]).map(l => ({
        productId: l.productId, description: (l.description as string)?.trim() || null, quantity: Number(l.quantity) || 0,
        materialCost: Number(l.materialCost) || 0, laborCost: Number(l.laborCost) || 0, machineCost: Number(l.machineCost) || 0,
        overheadCost: Number(l.overheadCost) || 0, wastagePercent: Number(l.wastagePercent) || 0, marginPercent: Number(l.marginPercent) || 0
      }))
    };
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.create(payload).subscribe({ next: done, error: err });
    else this.svc.update(this.editingId!, { id: this.editingId!, ...payload }).subscribe({ next: done, error: err });
  }

  send(q: QuotationListItemDto): void { this.rowAction(q, this.svc.send(q.id)); }
  accept(q: QuotationListItemDto): void { this.rowAction(q, this.svc.accept(q.id)); }
  reject(q: QuotationListItemDto): void { this.rowAction(q, this.svc.reject(q.id)); }
  revise(q: QuotationListItemDto): void { this.rowAction(q, this.svc.revise(q.id)); }
  convert(q: QuotationListItemDto): void { this.rowAction(q, this.svc.convert(q.id)); }
  generateProforma(q: QuotationListItemDto): void {
    this.rowAction(q, this.svc.generateProforma(q.id),
      'Proforma generated. Find it under Sales → Proforma Invoices.');
  }
  /** Rule: once a proforma is generated for a quotation, the SO must come from that proforma. */
  hasProforma(q: QuotationListItemDto): boolean { return !!q.convertedProformaInvoiceId; }

  private rowAction(q: QuotationListItemDto, obs: any, successMsg = ''): void {
    if (this.rowActionId) return;
    this.rowActionId = q.id; this.actionError = ''; this.actionMessage = ''; this.cdr.detectChanges();
    obs.subscribe({
      next: (res: any) => this.zone.run(() => { this.rowActionId = null; if (res.success) { if (successMsg) this.actionMessage = successMsg; this.load(); } else this.actionError = res.message || 'Action failed.'; this.cdr.detectChanges(); }),
      error: (e: any) => this.zone.run(() => { this.rowActionId = null; this.actionError = e?.error?.message || 'Action failed.'; this.cdr.detectChanges(); })
    });
  }

  confirmDelete(q: QuotationListItemDto): void { this.deleting = q; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.delete(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }

  statusClass(s: string): string { return s.toLowerCase(); }
  meta(c: AbstractControl) { return c.getRawValue(); }
}
