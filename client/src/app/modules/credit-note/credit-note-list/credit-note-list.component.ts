import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CreditNoteService } from '../../../services/credit-note.service';
import { CustomerInvoiceService } from '../../../services/customer-invoice.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  CreditNoteDto, CN_DN_STATUSES, CN_DN_REASONS
} from '../../../models/credit-debit-note.models';
import { CustomerInvoiceListItemDto } from '../../../models/customer-invoice.models';

@Component({
  selector: 'app-credit-note-list',
  standalone: false,
  templateUrl: './credit-note-list.component.html',
  styleUrl: './credit-note-list.component.scss'
})
export class CreditNoteListComponent implements OnInit {

  notes: CreditNoteDto[] = [];
  loading = false;
  totalCount = 0;
  actionError = '';
  actionMessage = '';
  rowActionId: number | null = null;

  readonly statuses = CN_DN_STATUSES;
  readonly reasons = CN_DN_REASONS;
  invoices: CustomerInvoiceListItemDto[] = [];

  filterStatus: string | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Dialog
  dialogVisible = false;
  dialogSaving = false;
  dialogError = '';
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  editing: CreditNoteDto | null = null;
  form!: FormGroup;
  selectedInvoice: CustomerInvoiceListItemDto | null = null;

  // Link to a posted Customer Return Note (set when navigated from the CRN screen)
  linkedCrnId: number | null = null;
  linkedCrnCode: string | null = null;
  linkCustomerId: number | null = null;
  linkCustomerName: string | null = null;

  // Cancel confirm
  cancelVisible = false;
  cancelling = false;
  cancelTarget: CreditNoteDto | null = null;
  cancelError = '';

  // Delete confirm (drafts)
  deleteVisible = false;
  deleting = false;
  deleteTarget: CreditNoteDto | null = null;
  deleteError = '';

  constructor(
    private service: CreditNoteService,
    private invoiceService: CustomerInvoiceService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      customerInvoiceId: [null, Validators.required],
      issueDate: [this.todayIso(), Validators.required],
      reason: ['PriceCorrection', Validators.required],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      notes: ['', Validators.maxLength(2000)]
    });
    this.loadInvoices();
    this.load();

    // Arrived from a posted CRN's "Create Credit Note" button — open a linked draft.
    const q = this.route.snapshot.queryParamMap;
    if (q.get('fromCrn')) {
      this.linkedCrnId = Number(q.get('fromCrn'));
      this.linkedCrnCode = q.get('crnCode');
      this.linkCustomerId = q.get('customerId') ? Number(q.get('customerId')) : null;
      this.linkCustomerName = q.get('customerName');
      // Strip the query params so a refresh doesn't re-trigger the linked dialog.
      this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });
      this.openCreate();
    }
  }

  /** Invoices offered in the create dialog — narrowed to the linked customer when present. */
  get dialogInvoices(): CustomerInvoiceListItemDto[] {
    return this.linkCustomerId
      ? this.invoices.filter(i => i.customerId === this.linkCustomerId)
      : this.invoices;
  }

  private todayIso(): string { return new Date().toISOString().substring(0, 10); }

  formatCurrency(amount: number, currency = 'BDT'): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency, maximumFractionDigits: 2 }).format(amount || 0);
  }

  reasonLabel(r: string): string { return this.reasons.find(x => x.value === r)?.label ?? r; }

  statusBadgeClass(s: string): string {
    switch (s) {
      case 'Draft': return 'draft';
      case 'Issued': return 'issued';
      case 'Cancelled': return 'cancelled';
      default: return '';
    }
  }

  private loadInvoices(): void {
    // Pull non-Draft, non-Cancelled invoices (i.e. eligible to receive a credit note)
    this.invoiceService.getAll({ page: 1, pageSize: 500, search: '' }).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          this.invoices = res.data.items.filter(i => i.status !== 'Draft' && i.status !== 'Cancelled');
        }
        this.cdr.detectChanges();
      })
    });
  }

  load(): void {
    this.loading = true;
    this.service.getAll(this.parameters, this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.notes = res.data.items;
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

  // ── Dialog ───────────────────────────────────────────────────────────────

  /** Header "New Credit Note" — always a fresh, unlinked draft. */
  newCreditNote(): void {
    this.clearLink();
    this.openCreate();
  }

  openCreate(): void {
    this.dialogMode = 'create';
    this.editing = null;
    this.selectedInvoice = null;
    this.dialogError = '';
    this.form.reset({
      customerInvoiceId: null,
      issueDate: this.todayIso(),
      // A return refund is a quality allowance by default; manual drafts keep PriceCorrection.
      reason: this.linkedCrnId ? 'QualityAllowance' : 'PriceCorrection',
      amount: 0,
      notes: this.linkedCrnId ? `Refund for return ${this.linkedCrnCode ?? ''}`.trim() : ''
    });
    this.dialogVisible = true;
  }

  onDialogHide(): void {
    // Clear the CRN link once the dialog is dismissed so the next manual draft is clean.
    this.clearLink();
  }

  private clearLink(): void {
    this.linkedCrnId = null;
    this.linkedCrnCode = null;
    this.linkCustomerId = null;
    this.linkCustomerName = null;
  }

  openEdit(n: CreditNoteDto): void {
    this.dialogMode = n.status === 'Draft' ? 'edit' : 'view';
    this.editing = n;
    this.selectedInvoice = this.invoices.find(i => i.id === n.customerInvoiceId) ?? null;
    this.dialogError = '';
    this.form.reset({
      customerInvoiceId: n.customerInvoiceId,
      issueDate: n.issueDate,
      reason: n.reason,
      amount: n.amount,
      notes: n.notes ?? ''
    });
    if (this.dialogMode === 'view') this.form.disable(); else this.form.enable();
    this.dialogVisible = true;
  }

  onInvoiceChange(ev: any): void {
    const id = ev?.value;
    this.selectedInvoice = this.invoices.find(i => i.id === id) ?? null;
    if (this.selectedInvoice) {
      const outstanding = this.selectedInvoice.amountDue;
      this.form.patchValue({ amount: Math.min(outstanding, this.form.get('amount')?.value || outstanding) });
    }
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const obs: any = this.dialogMode === 'create'
      ? this.service.create({
          customerInvoiceId: Number(v.customerInvoiceId),
          issueDate: v.issueDate,
          reason: v.reason,
          amount: Number(v.amount),
          notes: (v.notes as string)?.trim() || null,
          customerReturnNoteId: this.linkedCrnId
        })
      : this.service.update(this.editing!.id, {
          issueDate: v.issueDate,
          reason: v.reason,
          amount: Number(v.amount),
          notes: (v.notes as string)?.trim() || null
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

  // ── Issue ────────────────────────────────────────────────────────────────

  issue(n: CreditNoteDto): void {
    if (this.rowActionId) return;
    if (!confirm(`Issue credit note ${n.code} for ${this.formatCurrency(n.amount, n.currencyCode)}?\n\nThis will reduce outstanding on invoice ${n.customerInvoiceCode} and post Dr Sales Returns / Cr AR.`)) return;
    this.rowActionId = n.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.service.issue(n.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) { this.actionMessage = res.message || 'Issued.'; this.load(); }
        else this.actionError = res.message || 'Issue failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.rowActionId = null;
        this.actionError = err?.error?.message || 'Issue failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Cancel ───────────────────────────────────────────────────────────────

  openCancel(n: CreditNoteDto): void {
    this.cancelTarget = n;
    this.cancelError = '';
    this.cancelVisible = true;
  }

  doCancel(): void {
    if (!this.cancelTarget || this.cancelling) return;
    this.cancelling = true;
    this.cancelError = '';
    this.cdr.detectChanges();
    this.service.cancel(this.cancelTarget.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.cancelling = false;
        if (res.success) {
          this.cancelVisible = false;
          this.actionMessage = res.message || 'Cancelled.';
          this.load();
        } else {
          this.cancelError = res.message || 'Cancel failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.cancelling = false;
        this.cancelError = err?.error?.message || 'Cancel failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Delete (Draft only) ──────────────────────────────────────────────────

  openDelete(n: CreditNoteDto): void {
    this.deleteTarget = n;
    this.deleteError = '';
    this.deleteVisible = true;
  }

  doDelete(): void {
    if (!this.deleteTarget || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();
    this.service.delete(this.deleteTarget.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) {
          this.deleteVisible = false;
          this.actionMessage = res.message || 'Deleted.';
          this.load();
        } else {
          this.deleteError = res.message || 'Delete failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.deleting = false;
        this.deleteError = err?.error?.message || 'Delete failed.';
        this.cdr.detectChanges();
      })
    });
  }
}
