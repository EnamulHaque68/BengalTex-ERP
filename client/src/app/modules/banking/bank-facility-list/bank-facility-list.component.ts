import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { BankFacilityService } from '../../../services/bank-facility.service';
import { AuthService } from '../../../services/auth.service';
import {
  BankFacilityDto, BankFacilityDetailDto, FACILITY_TYPES, FACILITY_STATUSES, FACILITY_EVENT_TYPES
} from '../../../models/bank-facility.models';
import { PAYMENT_METHODS } from '../../../models/payment.models';
import { apiErrorMessage } from '../../../shared/utils/http-error.util';

/**
 * Phase A6c — bank treasury facilities register (term loan / OD-CC / FDR). Each facility is a
 * sub-ledger of financial events (drawdown, interest, repayment / placement, income, encashment)
 * that post journals; the detail dialog shows running balances + an inline add-event form.
 */
@Component({
  selector: 'app-bank-facility-list',
  standalone: false,
  templateUrl: './bank-facility-list.component.html',
  styleUrl: './bank-facility-list.component.scss'
})
export class BankFacilityListComponent implements OnInit {
  loading = false;
  canManage = false;
  actionError = '';
  actionMessage = '';

  facilities: BankFacilityDto[] = [];
  filterStatus: string | null = null;

  readonly types = FACILITY_TYPES;
  readonly statuses = FACILITY_STATUSES;
  readonly eventTypes = FACILITY_EVENT_TYPES;
  readonly paymentMethods = PAYMENT_METHODS;

  // Create dialog
  createVisible = false;
  saving = false;
  createError = '';
  facilityType = 'TermLoan';
  bankName = '';
  accountReference = '';
  amount = 0;
  interestRate = 0;
  startDate = '';
  maturityDate = '';
  notes = '';

  // Detail dialog
  detailVisible = false;
  detail: BankFacilityDetailDto | null = null;
  eventBusy = false;
  eventError = '';
  evType = 'Drawdown';
  evDate = '';
  evAmount = 0;
  evMethod = 'BankTransfer';
  evRef = '';

  constructor(
    private svc: BankFacilityService,
    private auth: AuthService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('Banking.Manage');
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.facilities = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  typeLabel(v: string): string { return this.types.find(t => t.value === v)?.label ?? v; }

  // ── Create ──
  openCreate(): void {
    this.facilityType = 'TermLoan'; this.bankName = ''; this.accountReference = '';
    this.amount = 0; this.interestRate = 0; this.startDate = this.todayIso(); this.maturityDate = '';
    this.notes = ''; this.createError = ''; this.createVisible = true;
  }

  doCreate(): void {
    if (this.saving || !this.bankName.trim() || this.amount <= 0 || !this.startDate) return;
    this.saving = true; this.createError = '';
    this.svc.create({
      facilityType: this.facilityType, bankName: this.bankName.trim(),
      accountReference: this.accountReference.trim() || null, amount: Number(this.amount) || 0,
      interestRate: Number(this.interestRate) || 0, startDate: this.startDate,
      maturityDate: this.maturityDate || null, notes: this.notes.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.saving = false;
        if (res.success) { this.createVisible = false; this.actionMessage = res.message || 'Facility created.'; this.load(); }
        else this.createError = res.message || 'Could not create the facility.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.saving = false; this.createError = apiErrorMessage(err, 'Could not create the facility.'); this.cdr.detectChanges(); })
    });
  }

  // ── Detail + events ──
  openDetail(f: BankFacilityDto): void {
    this.detail = null; this.eventError = '';
    this.evType = f.facilityType === 'Fdr' ? 'FdrPlacement' : 'Drawdown';
    this.evDate = this.todayIso(); this.evAmount = 0; this.evMethod = 'BankTransfer'; this.evRef = '';
    this.detailVisible = true;
    this.loadDetail(f.id);
  }

  private loadDetail(id: number): void {
    this.svc.getById(id).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.detail = res.data; this.cdr.detectChanges(); })
    });
  }

  /** Event types valid for the open facility (loan family vs FDR family). */
  get availableEventTypes() {
    if (!this.detail) return this.eventTypes;
    const fam = this.detail.facility.facilityType === 'Fdr' ? 'fdr' : 'loan';
    return this.eventTypes.filter(e => e.family === fam);
  }
  eventLabel(v: string): string { return this.eventTypes.find(e => e.value === v)?.label ?? v; }
  eventHint(v: string): string { return this.eventTypes.find(e => e.value === v)?.hint ?? ''; }

  addEvent(): void {
    if (!this.detail || this.eventBusy || this.evAmount <= 0 || !this.evDate) return;
    this.eventBusy = true; this.eventError = '';
    this.svc.addEvent(this.detail.facility.id, {
      eventType: this.evType, eventDate: this.evDate, amount: Number(this.evAmount) || 0,
      paymentMethod: this.evMethod, reference: this.evRef.trim() || null, notes: null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.eventBusy = false;
        if (res.success) { this.evAmount = 0; this.evRef = ''; this.loadDetail(this.detail!.facility.id); this.load(); }
        else this.eventError = res.message || 'Could not record the event.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.eventBusy = false; this.eventError = apiErrorMessage(err, 'Could not record the event.'); this.cdr.detectChanges(); })
    });
  }

  statusClass(s: string): string { return s === 'Active' ? 's-active' : 's-closed'; }
  formatMoney(v: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(v || 0);
  }
}
