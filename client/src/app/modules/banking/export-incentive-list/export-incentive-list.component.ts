import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ExportIncentiveService } from '../../../services/export-incentive.service';
import { AuthService } from '../../../services/auth.service';
import { ExportIncentiveClaimDto, INCENTIVE_STATUSES } from '../../../models/export-incentive.models';
import { PAYMENT_METHODS } from '../../../models/payment.models';
import { apiErrorMessage } from '../../../shared/utils/http-error.util';

/**
 * Phase A6b — export cash-incentive register. Accrue a claim (Dr 1186 / Cr 4260), mark it received
 * when the bank credits it (Dr Bank / Cr 1186), or cancel an un-received accrual.
 */
@Component({
  selector: 'app-export-incentive-list',
  standalone: false,
  templateUrl: './export-incentive-list.component.html',
  styleUrl: './export-incentive-list.component.scss'
})
export class ExportIncentiveListComponent implements OnInit {
  loading = false;
  canManage = false;
  actionError = '';
  actionMessage = '';

  claims: ExportIncentiveClaimDto[] = [];
  outstanding = 0;
  filterStatus: string | null = null;

  readonly statuses = INCENTIVE_STATUSES;
  readonly paymentMethods = PAYMENT_METHODS;

  // Accrue dialog
  createVisible = false;
  saving = false;
  createError = '';
  exportReference = '';
  incentiveRate = 0;
  amount = 0;
  claimDate = '';
  notes = '';

  // Received dialog
  receiveVisible = false;
  receiving = false;
  receiveError = '';
  receiveTarget: ExportIncentiveClaimDto | null = null;
  receivedDate = '';
  receivedMethod = 'BankTransfer';
  bankReference = '';

  constructor(
    private svc: ExportIncentiveService,
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
        if (res.success && res.data) { this.claims = res.data.items; this.outstanding = res.data.outstandingReceivable; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  openCreate(): void {
    this.exportReference = ''; this.incentiveRate = 0; this.amount = 0;
    this.claimDate = this.todayIso(); this.notes = ''; this.createError = '';
    this.createVisible = true;
  }

  doCreate(): void {
    if (this.saving || this.amount <= 0 || !this.claimDate) return;
    this.saving = true; this.createError = '';
    this.svc.create({
      customerInvoiceId: null, exportReference: this.exportReference.trim() || null,
      incentiveRate: Number(this.incentiveRate) || 0, amount: Number(this.amount) || 0,
      claimDate: this.claimDate, notes: this.notes.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.saving = false;
        if (res.success) { this.createVisible = false; this.actionMessage = res.message || 'Incentive accrued.'; this.load(); }
        else this.createError = res.message || 'Could not accrue the incentive.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.saving = false; this.createError = apiErrorMessage(err, 'Could not accrue the incentive.'); this.cdr.detectChanges(); })
    });
  }

  openReceive(c: ExportIncentiveClaimDto): void {
    this.receiveTarget = c; this.receivedDate = this.todayIso();
    this.receivedMethod = 'BankTransfer'; this.bankReference = ''; this.receiveError = '';
    this.receiveVisible = true;
  }

  doReceive(): void {
    if (!this.receiveTarget || this.receiving || !this.receivedDate) return;
    this.receiving = true; this.receiveError = '';
    this.svc.markReceived(this.receiveTarget.id, {
      receivedDate: this.receivedDate, paymentMethod: this.receivedMethod, bankReference: this.bankReference.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.receiving = false;
        if (res.success) { this.receiveVisible = false; this.actionMessage = res.message || 'Incentive received.'; this.load(); }
        else this.receiveError = res.message || 'Could not mark received.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.receiving = false; this.receiveError = apiErrorMessage(err, 'Could not mark received.'); this.cdr.detectChanges(); })
    });
  }

  cancel(c: ExportIncentiveClaimDto): void {
    if (this.loading) return;
    this.actionError = '';
    this.svc.cancel(c.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success) { this.actionMessage = res.message || 'Cancelled.'; this.load(); }
        else this.actionError = res.message || 'Cancel failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.actionError = apiErrorMessage(err, 'Cancel failed.'); this.cdr.detectChanges(); })
    });
  }

  statusClass(s: string): string { return s === 'Accrued' ? 's-accrued' : s === 'Received' ? 's-received' : 's-cancelled'; }
  formatMoney(v: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(v || 0);
  }
}
