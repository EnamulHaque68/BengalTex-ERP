import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ReceiptService } from '../../../services/receipt.service';
import { CustomerInvoiceService } from '../../../services/customer-invoice.service';
import { CustomerService } from '../../../services/customer.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { PAYMENT_METHODS, ReceiptDto, ReceiptListItemDto } from '../../../models/receipt.models';
import { CustomerInvoiceListItemDto } from '../../../models/customer-invoice.models';
import { CustomerListItemDto } from '../../../models/customer.models';

interface PayableInvoiceOption {
  id: number;
  code: string;
  customerName: string;
  totalAmount: number;
  amountDue: number;
  status: string;
  currencyCode: string;
  invoiceRate: number;       // the invoice's locked BDT rate
  displayLabel: string;
}

@Component({
  selector: 'app-receipt-list',
  standalone: false,
  templateUrl: './receipt-list.component.html',
  styleUrl: './receipt-list.component.scss'
})
export class ReceiptListComponent implements OnInit {

  receipts: ReceiptListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterCustomerId: number | null = null;
  filterPaymentMethod: string | null = null;
  actionError = '';
  actionMessage = '';

  // Email dialog
  emailDlgOpen = false;
  emailSourceId = 0;
  openEmail(row: { id: number }): void { this.emailSourceId = row.id; this.emailDlgOpen = true; }
  onEmailSent(ev: { sourceCode: string }): void { this.actionMessage = `Email sent for ${ev.sourceCode}.`; this.cdr.detectChanges(); }

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly paymentMethods = PAYMENT_METHODS;
  customers: CustomerListItemDto[] = [];
  payableInvoices: PayableInvoiceOption[] = [];
  selectedInvoice: PayableInvoiceOption | null = null;

  // Create dialog
  dialogVisible = false;
  dialogSaving = false;
  dialogError = '';
  form!: FormGroup;

  // View dialog (read-only details)
  viewDialogVisible = false;
  viewingReceipt: ReceiptDto | null = null;

  // Delete dialog
  deleteDialogVisible = false;
  deletingRct: ReceiptListItemDto | null = null;
  deleting = false;
  deleteError = '';

  constructor(
    private rctService: ReceiptService,
    private invService: CustomerInvoiceService,
    private customerService: CustomerService,
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
      customerInvoiceId: [null as number | null, Validators.required],
      receiptDate: [this.todayIso(), Validators.required],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      exchangeRate: [1, [Validators.required, Validators.min(0.000001)]],
      paymentMethod: ['Cash', Validators.required],
      referenceNumber: ['', Validators.maxLength(100)],
      notes: ['', Validators.maxLength(2000)]
    });
  }

  /** Selected invoice is in a foreign currency → the receipt-date rate matters (FX gain/loss). */
  get isForeignInvoice(): boolean {
    return !!this.selectedInvoice && this.selectedInvoice.currencyCode !== 'BDT';
  }

  /** Realized FX gain (+) or loss (−) in BDT = amount × (receiptRate − invoiceRate). */
  get fxPreview(): number {
    if (!this.isForeignInvoice || !this.selectedInvoice) return 0;
    const amount = Number(this.form.get('amount')?.value) || 0;
    const rate = Number(this.form.get('exchangeRate')?.value) || 0;
    return amount * (rate - this.selectedInvoice.invoiceRate);
  }

  private loadDropdowns(): void {
    this.customerService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.customers = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
  }

  private loadPayableInvoices(): void {
    this.invService.getAll({ page: 1, pageSize: 500, search: '' }).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.payableInvoices = res.data.items
              .filter(i => i.status === 'Issued' || i.status === 'PartiallyPaid')
              .map(i => ({
                id: i.id,
                code: i.code,
                customerName: i.customerName,
                totalAmount: i.totalAmount,
                amountDue: i.amountDue,
                status: i.status,
                currencyCode: i.currencyCode,
                invoiceRate: i.exchangeRate,
                displayLabel: `${i.code} — ${i.customerName} (outstanding ${this.formatMoney(i.amountDue, i.currencyCode)})`
              }));
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.rctService.getAll(
      this.parameters,
      undefined,
      this.filterCustomerId ?? undefined,
      this.filterPaymentMethod ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.receipts = res.data.items;
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

  openCreate(): void {
    this.dialogError = '';
    this.selectedInvoice = null;
    this.form.enable();
    this.form.reset({
      customerInvoiceId: null,
      receiptDate: this.todayIso(),
      amount: 0,
      exchangeRate: 1,
      paymentMethod: 'Cash',
      referenceNumber: '',
      notes: ''
    });
    this.loadPayableInvoices();
    this.dialogVisible = true;
  }

  onInvoiceChange(event: any): void {
    const id = event?.value;
    this.selectedInvoice = id ? (this.payableInvoices.find(i => i.id === id) ?? null) : null;
    if (this.selectedInvoice) {
      // default amount to full outstanding + seed the rate from the invoice's locked rate
      this.form.patchValue({
        amount: this.selectedInvoice.amountDue,
        exchangeRate: this.selectedInvoice.invoiceRate
      });
    }
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();
    this.rctService.create({
      customerInvoiceId: v.customerInvoiceId,
      receiptDate: v.receiptDate,
      amount: Number(v.amount),
      paymentMethod: v.paymentMethod,
      referenceNumber: (v.referenceNumber as string)?.trim() || null,
      notes: (v.notes as string)?.trim() || null,
      // Only send a rate for foreign-currency invoices; BDT invoices ignore it (rate = 1).
      exchangeRate: this.isForeignInvoice ? Number(v.exchangeRate) : null
    }).subscribe({
      next: (res) => this.handleSave(res),
      error: (err) => this.handleError(err)
    });
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

  openView(rct: ReceiptListItemDto): void {
    this.viewingReceipt = null;
    this.viewDialogVisible = true;
    this.rctService.getById(rct.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.viewingReceipt = res.data;
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  confirmDelete(rct: ReceiptListItemDto): void {
    this.deletingRct = rct;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingRct || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.rctService.delete(this.deletingRct.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingRct = null;
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

  paymentMethodLabel(value: string): string {
    return this.paymentMethods.find(p => p.value === value)?.label ?? value;
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
      return new Intl.NumberFormat('en-US', { style: 'currency', currency: c, maximumFractionDigits: 2 }).format(amount || 0);
    } catch {
      return `${(amount || 0).toLocaleString('en-US', { maximumFractionDigits: 2 })} ${c}`;
    }
  }
}
