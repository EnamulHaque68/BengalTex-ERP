import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LetterOfCreditService } from '../../../services/letter-of-credit.service';
import { SupplierService } from '../../../services/supplier.service';
import { CurrencyService } from '../../../services/currency.service';
import { PurchaseOrderService } from '../../../services/purchase-order.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  LetterOfCreditDto, LetterOfCreditListItemDto, LC_STATUSES, LC_TYPES,
  LC_EVENT_TYPES, LcFinancialEventDto, LcEventsSummaryDto
} from '../../../models/letter-of-credit.models';
import { PAYMENT_METHODS } from '../../../models/payment.models';
import { SupplierListItemDto } from '../../../models/supplier.models';
import { CurrencyDto } from '../../../models/master-data.models';
import { PurchaseOrderListItemDto } from '../../../models/purchase-order.models';

@Component({
  selector: 'app-lc-list',
  standalone: false,
  templateUrl: './lc-list.component.html',
  styleUrl: './lc-list.component.scss'
})
export class LcListComponent implements OnInit {

  lcs: LetterOfCreditListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;
  rowActionId: number | null = null;

  readonly statuses = LC_STATUSES;
  readonly lcTypes = LC_TYPES;

  get isBackToBack(): boolean { return this.form?.get('type')?.value === 'BackToBack'; }
  suppliers: SupplierListItemDto[] = [];
  currencies: CurrencyDto[] = [];
  purchaseOrders: PurchaseOrderListItemDto[] = [];

  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deletingLc: LetterOfCreditListItemDto | null = null;
  deleting = false;
  deleteError = '';

  /** Full LC detail (with utilisation summary + related GRNs) shown while editing. */
  loadedLc: LetterOfCreditDto | null = null;

  // Phase A6a — LC financial events (shown inside the edit dialog for an Open+ LC)
  readonly eventTypes = LC_EVENT_TYPES;
  readonly paymentMethods = PAYMENT_METHODS;
  lcEvents: LcFinancialEventDto[] = [];
  lcSummary: LcEventsSummaryDto | null = null;
  eventBusy = false;
  eventError = '';
  evType = 'MarginDeposit';
  evDate = '';
  evAmount = 0;
  evMargin = 0;
  evMethod = 'BankTransfer';
  evRef = '';

  constructor(
    private service: LetterOfCreditService,
    private supplierService: SupplierService,
    private currencyService: CurrencyService,
    private poService: PurchaseOrderService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  /** Jump to a related goods receipt's details (LC → GRN traceability). */
  goToGoodsReceipt(grnId: number): void {
    this.dialogVisible = false;
    this.router.navigate(['/goods-receipts'], { queryParams: { open: grnId } });
  }

  ngOnInit(): void {
    this.buildForm();
    this.loadDropdowns();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  private buildForm(): void {
    this.form = this.fb.group({
      code: [''],
      type: ['Import', Validators.required],
      masterLcReference: ['', Validators.maxLength(100)],
      masterLcBuyer: ['', Validators.maxLength(200)],
      lcNumber: ['', [Validators.required, Validators.maxLength(100)]],
      issuingBank: ['', [Validators.required, Validators.maxLength(200)]],
      supplierId: [null as number | null, Validators.required],
      purchaseOrderId: [null as number | null],
      currencyId: [null as number | null, Validators.required],
      exchangeRate: [1, [Validators.required, Validators.min(0.000001)]],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      issueDate: [this.todayIso(), Validators.required],
      expiryDate: [this.todayIso(), Validators.required],
      tenorDays: [90, [Validators.required, Validators.min(0)]],
      notes: ['', Validators.maxLength(2000)]
    });
  }

  formatMoney(amount: number, code: string | null | undefined): string {
    const c = code || 'BDT';
    try { return new Intl.NumberFormat('en-US', { style: 'currency', currency: c, maximumFractionDigits: 2 }).format(amount || 0); }
    catch { return `${(amount || 0).toLocaleString('en-US', { maximumFractionDigits: 2 })} ${c}`; }
  }
  get currentCurrencyCode(): string {
    const id = this.form?.get('currencyId')?.value;
    return this.currencies.find(c => c.id === id)?.code || 'BDT';
  }
  formatDate(d: string | null): string { return d || '—'; }

  onCurrencyChange(event: any): void {
    const cur = this.currencies.find(c => c.id === event?.value);
    if (cur) this.form.get('exchangeRate')?.setValue(cur.exchangeRateToBase);
  }

  private loadDropdowns(): void {
    this.supplierService.getAll({ page: 1, pageSize: 1000, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.suppliers = res.data.items; this.cdr.detectChanges(); })
    });
    this.currencyService.getAll(false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.currencies = res.data; this.cdr.detectChanges(); })
    });
    this.poService.getAll({ page: 1, pageSize: 500, search: '' }).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.purchaseOrders = res.data.items; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.service.getAll(this.parameters, undefined, this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.lcs = res.data.items; this.totalCount = res.data.totalCount; }
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

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.loadedLc = null;
    this.form.reset({
      code: '', type: 'Import', masterLcReference: '', masterLcBuyer: '',
      lcNumber: '', issuingBank: '', supplierId: null, purchaseOrderId: null,
      currencyId: null, exchangeRate: 1, amount: 0, issueDate: this.todayIso(), expiryDate: this.todayIso(),
      tenorDays: 90, notes: ''
    });
    this.dialogVisible = true;
  }

  openEdit(lc: LetterOfCreditListItemDto): void {
    this.dialogMode = 'edit';
    this.editingId = lc.id;
    this.dialogError = '';
    this.loadedLc = null;
    this.lcEvents = [];
    this.lcSummary = null;
    this.eventError = '';
    this.evType = 'MarginDeposit';
    this.evDate = this.todayIso();
    this.evAmount = 0; this.evMargin = 0; this.evMethod = 'BankTransfer'; this.evRef = '';
    this.dialogVisible = true;
    this.service.getById(lc.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const l = res.data;
          this.loadedLc = l;
          if (l.status !== 'Draft' && l.status !== 'Cancelled') this.loadEvents(l.id);
          this.form.patchValue({
            code: l.code, type: l.type ?? 'Import',
            masterLcReference: l.masterLcReference ?? '', masterLcBuyer: l.masterLcBuyer ?? '',
            lcNumber: l.lcNumber, issuingBank: l.issuingBank, supplierId: l.supplierId,
            purchaseOrderId: l.purchaseOrderId, currencyId: l.currencyId, exchangeRate: l.exchangeRate,
            amount: l.amount, issueDate: l.issueDate, expiryDate: l.expiryDate, tenorDays: l.tenorDays, notes: l.notes ?? ''
          });
          this.cdr.detectChanges();
        }
      })
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const common = {
      lcNumber: (v.lcNumber as string).trim(),
      issuingBank: (v.issuingBank as string).trim(),
      supplierId: v.supplierId,
      purchaseOrderId: v.purchaseOrderId ?? null,
      currencyId: v.currencyId,
      exchangeRate: Number(v.exchangeRate) || 1,
      amount: Number(v.amount) || 0,
      issueDate: v.issueDate,
      expiryDate: v.expiryDate,
      tenorDays: Number(v.tenorDays) || 0,
      notes: (v.notes as string)?.trim() || null,
      type: v.type as string,
      masterLcReference: v.type === 'BackToBack' ? ((v.masterLcReference as string)?.trim() || null) : null,
      masterLcBuyer: v.type === 'BackToBack' ? ((v.masterLcBuyer as string)?.trim() || null) : null
    };
    const obs = this.dialogMode === 'create'
      ? this.service.create({ ...common, code: (v.code as string)?.trim() || null })
      : this.service.update(this.editingId!, common);
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

  // ─── Lifecycle actions ─────────────────────────────────────────────────────

  open(lc: LetterOfCreditListItemDto): void { this.runRowAction(lc, this.service.open.bind(this.service)); }
  ship(lc: LetterOfCreditListItemDto): void { this.runRowAction(lc, this.service.ship.bind(this.service)); }
  settle(lc: LetterOfCreditListItemDto): void { this.runRowAction(lc, this.service.settle.bind(this.service)); }
  cancel(lc: LetterOfCreditListItemDto): void { this.runRowAction(lc, this.service.cancel.bind(this.service)); }

  private runRowAction(lc: LetterOfCreditListItemDto, fn: (id: number) => any): void {
    if (this.rowActionId) return;
    this.rowActionId = lc.id;
    this.actionError = '';
    this.cdr.detectChanges();
    fn(lc.id).subscribe({
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

  confirmDelete(lc: LetterOfCreditListItemDto): void {
    this.deletingLc = lc;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }
  doDelete(): void {
    if (!this.deletingLc || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();
    this.service.delete(this.deletingLc.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) { this.deleteDialogVisible = false; this.deletingLc = null; this.load(); }
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

  // ─── Phase A6a — LC financial events ───────────────────────────────────────

  get isRetirementEvent(): boolean { return this.evType === 'RetirementSight' || this.evType === 'AcceptanceUsance'; }
  eventLabel(v: string): string { return this.eventTypes.find(e => e.value === v)?.label ?? v; }
  eventHint(v: string): string { return this.eventTypes.find(e => e.value === v)?.hint ?? ''; }

  loadEvents(id: number): void {
    this.service.getEvents(id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) { this.lcEvents = res.data.events; this.lcSummary = res.data.summary; }
        this.cdr.detectChanges();
      })
    });
  }

  addEvent(): void {
    if (!this.editingId || this.eventBusy || this.evAmount <= 0 || !this.evDate) return;
    this.eventBusy = true; this.eventError = '';
    this.service.addEvent(this.editingId, {
      eventType: this.evType, eventDate: this.evDate, amount: Number(this.evAmount) || 0,
      marginApplied: this.isRetirementEvent ? (Number(this.evMargin) || 0) : 0,
      paymentMethod: this.evMethod, reference: this.evRef.trim() || null, notes: null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.eventBusy = false;
        if (res.success) {
          this.evAmount = 0; this.evMargin = 0; this.evRef = '';
          this.loadEvents(this.editingId!);
          this.service.getById(this.editingId!).subscribe({
            next: (r) => this.zone.run(() => { if (r.success && r.data) this.loadedLc = r.data; this.cdr.detectChanges(); })
          });
          this.load();   // status may have advanced
        } else this.eventError = res.message || 'Could not record the event.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.eventBusy = false; this.eventError = err?.error?.message || 'Could not record the event.'; this.cdr.detectChanges(); })
    });
  }

  canEdit(l: LetterOfCreditListItemDto): boolean { return l.status === 'Draft' || l.status === 'Open'; }
  canOpen(l: LetterOfCreditListItemDto): boolean { return l.status === 'Draft'; }
  canShip(l: LetterOfCreditListItemDto): boolean { return l.status === 'Open'; }
  canSettle(l: LetterOfCreditListItemDto): boolean { return l.status === 'Shipped'; }
  canCancel(l: LetterOfCreditListItemDto): boolean { return l.status === 'Draft' || l.status === 'Open'; }
  canDelete(l: LetterOfCreditListItemDto): boolean { return l.status === 'Draft' || l.status === 'Cancelled'; }
}
