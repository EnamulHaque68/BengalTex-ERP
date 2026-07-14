import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { StatutoryService } from '../../../services/statutory.service';
import { AuthService } from '../../../services/auth.service';
import {
  StatutoryLiabilityDto, StatutoryRemittanceDto, STATUTORY_TAX_TYPES
} from '../../../models/statutory.models';
import { PAYMENT_METHODS } from '../../../models/payment.models';
import { apiErrorMessage } from '../../../shared/utils/http-error.util';

/**
 * Phase A5b — statutory liabilities: outstanding AIT / VDS / PF payable balances and the
 * remittance (challan) register that clears them (Dr payable / Cr Cash|Bank).
 */
@Component({
  selector: 'app-statutory-liabilities',
  standalone: false,
  templateUrl: './statutory-liabilities.component.html',
  styleUrl: './statutory-liabilities.component.scss'
})
export class StatutoryLiabilitiesComponent implements OnInit {
  loading = false;
  canManage = false;
  actionError = '';
  actionMessage = '';

  liabilities: StatutoryLiabilityDto[] = [];
  remittances: StatutoryRemittanceDto[] = [];

  readonly taxTypes = STATUTORY_TAX_TYPES;
  readonly paymentMethods = PAYMENT_METHODS;

  // Remit dialog
  dialogVisible = false;
  saving = false;
  dialogError = '';
  taxType = 'Ait';
  periodYear: number;
  periodMonth: number;
  amount = 0;
  remittanceDate = '';
  paymentMethod = 'BankTransfer';
  challanNo = '';
  notes = '';

  constructor(
    private svc: StatutoryService,
    private auth: AuthService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {
    const now = new Date();
    this.periodYear = now.getFullYear();
    this.periodMonth = now.getMonth() + 1;
  }

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('Accounting.CloseBooks');
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.liabilities().subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.liabilities = res.data.items;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
    this.svc.remittances().subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.remittances = res.data; this.cdr.detectChanges(); })
    });
  }

  taxLabel(v: string): string { return this.taxTypes.find(t => t.value === v)?.label ?? v; }

  openRemit(item?: StatutoryLiabilityDto): void {
    const now = new Date();
    this.taxType = item?.taxType ?? 'Ait';
    this.amount = item ? Math.max(0, item.outstanding) : 0;
    this.periodYear = now.getFullYear();
    this.periodMonth = now.getMonth() + 1;
    this.remittanceDate = now.toISOString().slice(0, 10);
    this.paymentMethod = 'BankTransfer';
    this.challanNo = '';
    this.notes = '';
    this.dialogError = '';
    this.dialogVisible = true;
  }

  doRemit(): void {
    if (this.saving || this.amount <= 0 || !this.remittanceDate) return;
    this.saving = true; this.dialogError = '';
    this.svc.remit({
      taxType: this.taxType, periodYear: this.periodYear, periodMonth: this.periodMonth,
      amount: Number(this.amount) || 0, remittanceDate: this.remittanceDate,
      paymentMethod: this.paymentMethod, challanNo: this.challanNo.trim() || null,
      notes: this.notes.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.saving = false;
        if (res.success) { this.dialogVisible = false; this.actionMessage = res.message || 'Remitted.'; this.load(); }
        else this.dialogError = res.message || 'Remittance failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.saving = false; this.dialogError = apiErrorMessage(err, 'Remittance failed.'); this.cdr.detectChanges(); })
    });
  }

  cardClass(taxType: string): string {
    return taxType === 'Ait' ? 'c-ait' : taxType === 'Vds' ? 'c-vds' : 'c-pf';
  }

  formatMoney(v: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(v || 0);
  }

  ym(y: number, m: number): string { return `${y}-${m.toString().padStart(2, '0')}`; }
}
