import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SalesOrderService } from '../../../services/sales-order.service';
import { CustomerService } from '../../../services/customer.service';
import { ProductService } from '../../../services/product.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { SO_STATUSES, SalesOrderDto, SalesOrderLineDto, SalesOrderListItemDto } from '../../../models/sales-order.models';
import { CustomerListItemDto, CustomerCreditStatusDto } from '../../../models/customer.models';
import { ProductListItemDto } from '../../../models/product.models';
import { CurrencyDto } from '../../../models/master-data.models';
import { CurrencyService } from '../../../services/currency.service';
import { CustomerPricingService } from '../../../services/customer-pricing.service';

@Component({
  selector: 'app-sales-order-list',
  standalone: false,
  templateUrl: './sales-order-list.component.html',
  styleUrl: './sales-order-list.component.scss'
})
export class SalesOrderListComponent implements OnInit {

  sos: SalesOrderListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterCustomerId: number | null = null;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Dropdown sources
  readonly statuses = SO_STATUSES;
  customers: CustomerListItemDto[] = [];
  products: ProductListItemDto[] = [];
  currencies: CurrencyDto[] = [];

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Phase 1 — loaded SO detail (used by the view-mode production-progress panel)
  loadedDetail: SalesOrderDto | null = null;

  // Credit standing of the selected customer (shown on the create dialog)
  creditStatus: CustomerCreditStatusDto | null = null;
  creditLoading = false;

  // Delete
  deleteDialogVisible = false;
  deletingSo: SalesOrderListItemDto | null = null;
  deleting = false;
  deleteError = '';

  // Row action (confirm / cancel) in-flight id
  rowActionId: number | null = null;

  constructor(
    private soService: SalesOrderService,
    private customerService: CustomerService,
    private productService: ProductService,
    private currencyService: CurrencyService,
    private pricingService: CustomerPricingService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadDropdowns();
    this.load();

    // Traceability deep-link: /sales-orders?open=<id> opens that SO's details.
    const open = Number(this.route.snapshot.queryParamMap.get('open'));
    if (open > 0) this.openEdit({ id: open } as SalesOrderListItemDto);
  }

  /** Jump to a related customer invoice's details (SO → invoice traceability). */
  goToInvoice(invoiceId: number): void {
    this.dialogVisible = false;
    this.router.navigate(['/customer-invoices'], { queryParams: { open: invoiceId } });
  }

  /** Remaining-to-invoice on a line (Ordered − Invoiced). */
  lineRemainingToInvoice(l: SalesOrderLineDto): number {
    return Math.max(0, (l.quantity || 0) - (l.invoicedQuantity || 0));
  }

  invoiceBadgeClass(status: string | undefined): string {
    switch (status) {
      case 'FullyInvoiced': return 'inv-full';
      case 'PartiallyInvoiced': return 'inv-partial';
      default: return 'inv-none';
    }
  }
  invoiceStatusLabel(status: string | undefined): string {
    switch (status) {
      case 'FullyInvoiced': return 'Fully Invoiced';
      case 'PartiallyInvoiced': return 'Partially Invoiced';
      default: return 'Not Invoiced';
    }
  }

  // ─── Form ────────────────────────────────────────────────────────────────

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private buildForm(): void {
    this.form = this.fb.group({
      customerId: [null as number | null, Validators.required],
      orderDate: [this.todayIso(), Validators.required],
      requiredDeliveryDate: [null as string | null],
      customerPoRef: ['', Validators.maxLength(100)],
      deliveryAddress: ['', Validators.maxLength(500)],
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
    productId: number | null = null,
    quantity = 1,
    unitPrice = 0,
    lineNotes = ''
  ): FormGroup {
    return this.fb.group({
      productId: [productId, Validators.required],
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

  onLineProductChange(line: AbstractControl, event: any): void {
    const productId = event?.value;
    const product = this.productById(productId);
    if (product) line.get('unitPrice')?.setValue(product.salesPrice);

    // Override with the customer-specific price when one exists for this buyer + product.
    const customerId = this.form.get('customerId')?.value;
    if (customerId && productId) {
      this.pricingService.lookup(customerId, productId).subscribe({
        next: (res) => this.zone.run(() => {
          if (res.success && res.data != null) line.get('unitPrice')?.setValue(res.data);
          this.cdr.detectChanges();
        })
      });
    }
  }

  // ─── Line computations ───────────────────────────────────────────────────

  productById(id: number | null | undefined): ProductListItemDto | undefined {
    return id ? this.products.find(p => p.id === id) : undefined;
  }

  lineUomCode(line: AbstractControl): string {
    return this.productById(line.get('productId')?.value)?.unitOfMeasureCode ?? '—';
  }

  lineTotal(line: AbstractControl): number {
    const qty = Number(line.get('quantity')?.value) || 0;
    const price = Number(line.get('unitPrice')?.value) || 0;
    return qty * price;
  }

  totalAmount(): number {
    return this.lines.controls.reduce((sum, l) => sum + this.lineTotal(l), 0);
  }

  /** This order's line total converted to base BDT (for comparison against available credit). */
  get orderTotalBdt(): number {
    return this.totalAmount() * (Number(this.form.get('exchangeRate')?.value) || 1);
  }

  /** True when credit control is enforced and this order would push the customer over their limit. */
  get creditWouldExceed(): boolean {
    if (!this.creditStatus?.enforced) return false;
    return this.creditStatus.outstandingAr + this.orderTotalBdt > this.creditStatus.creditLimit;
  }

  /** Fetch the picked customer's credit standing to show on the dialog. */
  onCustomerChange(ev: any): void {
    const id = ev?.value;
    this.creditStatus = null;
    if (!id) return;
    this.creditLoading = true;
    this.customerService.getCreditStatus(id).subscribe({
      next: (res) => this.zone.run(() => {
        this.creditLoading = false;
        if (res.success && res.data) this.creditStatus = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.creditLoading = false; this.cdr.detectChanges(); })
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency',
      currency: 'BDT',
      maximumFractionDigits: 2
    }).format(amount || 0);
  }

  formatDate(d: string | null): string {
    return d ? d : '—';
  }

  // ─── Data loading ────────────────────────────────────────────────────────

  private loadDropdowns(): void {
    this.customerService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.customers = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
    this.productService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.products = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
    this.currencyService.getAll(false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.currencies = res.data;
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

  get currentCurrencyCode(): string {
    return this.currencyCodeById(this.form?.get('currencyId')?.value) || 'BDT';
  }

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

  load(): void {
    this.loading = true;
    this.soService.getAll(
      this.parameters,
      this.filterCustomerId ?? undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.sos = res.data.items;
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
    this.creditStatus = null;
    this.loadedDetail = null;
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      customerId: this.filterCustomerId ?? null,
      orderDate: this.todayIso(),
      requiredDeliveryDate: null,
      customerPoRef: '',
      deliveryAddress: '',
      currencyId: this.baseCurrencyId(),
      exchangeRate: 1,
      notes: ''
    });
    // If a customer filter pre-seeds the form, fetch their credit standing up front.
    if (this.filterCustomerId) this.onCustomerChange({ value: this.filterCustomerId });
    this.addLine();
    this.dialogVisible = true;
  }

  openEdit(so: SalesOrderListItemDto): void {
    this.editingId = so.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.creditStatus = null;
    this.form.enable();
    this.lines.clear();
    this.dialogVisible = true;

    this.soService.getById(so.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const s = res.data;
            this.loadedDetail = s;
            this.dialogMode = s.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              customerId: s.customerId,
              orderDate: s.orderDate,
              requiredDeliveryDate: s.requiredDeliveryDate ?? null,
              customerPoRef: s.customerPoRef ?? '',
              deliveryAddress: s.deliveryAddress ?? '',
              currencyId: s.currencyId,
              exchangeRate: s.exchangeRate,
              notes: s.notes ?? ''
            });
            s.lines.forEach(l => this.lines.push(
              this.newLine(l.productId, l.quantity, l.unitPrice, l.lineNotes ?? '')
            ));
            if (this.dialogMode === 'view') this.form.disable();
            else this.onCustomerChange({ value: s.customerId });
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
      productId: l.productId,
      quantity: Number(l.quantity) || 0,
      unitPrice: Number(l.unitPrice) || 0,
      lineNotes: (l.lineNotes as string)?.trim() || null
    }));

    const baseFields = {
      customerId: v.customerId,
      orderDate: v.orderDate,
      requiredDeliveryDate: v.requiredDeliveryDate || null,
      customerPoRef: (v.customerPoRef as string)?.trim() || null,
      deliveryAddress: (v.deliveryAddress as string)?.trim() || null,
      currencyId: v.currencyId,
      exchangeRate: Number(v.exchangeRate) || 1,
      notes: (v.notes as string)?.trim() || null,
      lines
    };

    if (this.dialogMode === 'create') {
      this.soService.create(baseFields).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.soService.update(this.editingId, baseFields).subscribe({
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

  confirm(so: SalesOrderListItemDto): void {
    this.runRowAction(so, this.soService.confirm.bind(this.soService));
  }

  cancel(so: SalesOrderListItemDto): void {
    this.runRowAction(so, this.soService.cancel.bind(this.soService));
  }

  close(so: SalesOrderListItemDto): void {
    this.runRowAction(so, this.soService.close.bind(this.soService));
  }

  canClose(so: SalesOrderListItemDto): boolean {
    return so.status === 'PartiallyDispatched' || so.status === 'Dispatched' || so.status === 'Delivered';
  }

  private runRowAction(so: SalesOrderListItemDto, fn: (id: number) => any): void {
    if (this.rowActionId) return;
    this.rowActionId = so.id;
    this.actionError = '';
    this.cdr.detectChanges();
    fn(so.id).subscribe({
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

  canCancel(so: SalesOrderListItemDto): boolean {
    return so.status === 'Draft' || so.status === 'Confirmed';
  }

  canDelete(so: SalesOrderListItemDto): boolean {
    return so.status === 'Draft' || so.status === 'Cancelled';
  }

  // ─── Phase 1: production progress display ─────────────────────────────────

  /** CSS modifier for the production-status badge. */
  productionBadgeClass(status: string | undefined): string {
    switch (status) {
      case 'Produced':          return 'produced';
      case 'PartiallyProduced': return 'partial';
      case 'Planning':          return 'planning';
      default:                  return 'notstarted';   // NotStarted
    }
  }

  productionStatusLabel(status: string | undefined): string {
    switch (status) {
      case 'PartiallyProduced': return 'Partial';
      case 'NotStarted':        return 'Not started';
      default:                  return status || 'Not started';
    }
  }

  /** Remaining still to produce for a line = ordered − produced. */
  lineRemainingToProduce(line: SalesOrderLineDto): number {
    return (line.quantity ?? 0) - (line.producedQuantity ?? 0);
  }

  // ─── Delete ──────────────────────────────────────────────────────────────

  confirmDelete(so: SalesOrderListItemDto): void {
    this.deletingSo = so;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingSo || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.soService.delete(this.deletingSo.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingSo = null;
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
