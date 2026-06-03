import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService } from '../../../services/accounting.service';
import { MasterSetupService } from '../../../services/master-setup.service';
import { CashBookDto } from '../../../models/accounting.models';
import { BankAccountDto } from '../../../models/master-setup.models';

@Component({
  selector: 'app-bank-book',
  standalone: false,
  templateUrl: './bank-book.component.html',
  styleUrl: './bank-book.component.scss'
})
export class BankBookComponent implements OnInit {
  fromDate: string;
  toDate: string;
  bankAccountId: number | null = null;          // null = aggregate over Bank ledger (1120)
  bankAccounts: BankAccountDto[] = [];
  data: CashBookDto | null = null;
  loading = false;
  error = '';

  constructor(
    private svc: AccountingService,
    private masterSvc: MasterSetupService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {
    const now = new Date();
    this.fromDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
    this.toDate = now.toISOString().slice(0, 10);
  }

  ngOnInit(): void {
    this.masterSvc.getBankAccounts(false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.bankAccounts = res.data;
        this.cdr.detectChanges();
      })
    });
    this.run();
  }

  run(): void {
    this.error = '';
    this.loading = true;
    this.svc.bankBook(this.fromDate, this.toDate, this.bankAccountId ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success) this.data = res.data ?? null;
        else { this.data = null; this.error = res.message || 'Failed.'; }
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => {
        this.loading = false;
        this.error = e?.error?.message || 'Failed.';
        this.cdr.detectChanges();
      })
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }
}
