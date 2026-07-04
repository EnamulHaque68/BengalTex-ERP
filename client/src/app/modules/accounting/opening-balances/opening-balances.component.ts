import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService, OpeningBalanceAccountDto } from '../../../services/accounting.service';

import { apiErrorMessage } from '../../../shared/utils/http-error.util';
interface ObRow extends OpeningBalanceAccountDto {
  debit: number;
  credit: number;
}

/**
 * Phase A1 — Ledger opening-balance import. Per-account Dr/Cr grid → one posted Opening
 * voucher; any imbalance auto-plugs to Opening Balance Equity (3150). AR/AP party-wise detail
 * is entered as opening invoices (IsOpening flag) whose journals are suppressed.
 */
@Component({
  selector: 'app-opening-balances',
  standalone: false,
  templateUrl: './opening-balances.component.html',
  styleUrl: './opening-balances.component.scss'
})
export class OpeningBalancesComponent implements OnInit {
  rows: ObRow[] = [];
  loading = false;
  saving = false;
  error = '';
  message = '';
  asOfDate = '';
  search = '';

  constructor(
    private svc: AccountingService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.asOfDate = new Date().toISOString().slice(0, 10);
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.openingBalanceTemplate().subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.rows = res.data.map(a => ({ ...a, debit: 0, credit: 0 }));
        }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  get filtered(): ObRow[] {
    const q = this.search.trim().toLowerCase();
    return !q ? this.rows
      : this.rows.filter(r => r.code.toLowerCase().includes(q) || r.name.toLowerCase().includes(q));
  }

  get totalDebit(): number { return this.rows.reduce((s, r) => s + (Number(r.debit) || 0), 0); }
  get totalCredit(): number { return this.rows.reduce((s, r) => s + (Number(r.credit) || 0), 0); }
  get imbalance(): number { return this.totalDebit - this.totalCredit; }
  get hasExistingOpening(): boolean { return this.rows.some(r => r.currentDebit > 0 || r.currentCredit > 0); }
  get enteredCount(): number { return this.rows.filter(r => (Number(r.debit) || 0) > 0 || (Number(r.credit) || 0) > 0).length; }

  import(): void {
    if (this.saving || this.enteredCount === 0 || !this.asOfDate) return;
    this.saving = true;
    this.error = '';
    this.message = '';

    const lines = this.rows
      .filter(r => (Number(r.debit) || 0) > 0 || (Number(r.credit) || 0) > 0)
      .map(r => ({ accountId: r.accountId, debit: Number(r.debit) || 0, credit: Number(r.credit) || 0 }));

    this.svc.importOpeningBalances(this.asOfDate, lines).subscribe({
      next: (res) => this.zone.run(() => {
        this.saving = false;
        if (res.success) { this.message = res.message || 'Opening voucher posted.'; this.load(); }
        else this.error = res.message || 'Import failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.saving = false;
        this.error = apiErrorMessage(err, 'Import failed.');
        this.cdr.detectChanges();
      })
    });
  }

  formatMoney(v: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 })
      .format(v || 0);
  }
}
