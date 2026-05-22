import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService } from '../../../services/accounting.service';
import { AccountDto, GeneralLedgerDto } from '../../../models/accounting.models';

@Component({
  selector: 'app-general-ledger',
  standalone: false,
  templateUrl: './general-ledger.component.html',
  styleUrl: './general-ledger.component.scss'
})
export class GeneralLedgerComponent implements OnInit {
  accounts: AccountDto[] = [];
  accountId: number | null = null;
  fromDate: string;
  toDate: string;
  data: GeneralLedgerDto | null = null;
  loading = false;
  error = '';

  constructor(private svc: AccountingService, private zone: NgZone, private cdr: ChangeDetectorRef) {
    const now = new Date();
    this.fromDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
    this.toDate = now.toISOString().slice(0, 10);
  }

  ngOnInit(): void {
    this.svc.getAccounts(undefined, false, true).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.accounts = res.data; this.cdr.detectChanges(); })
    });
  }

  run(): void {
    this.error = '';
    if (!this.accountId) { this.error = 'Select an account.'; return; }
    this.loading = true;
    this.svc.generalLedger(this.accountId, this.fromDate, this.toDate).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success) this.data = res.data ?? null; else { this.data = null; this.error = res.message || 'Failed.'; }
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => { this.loading = false; this.error = e?.error?.message || 'Failed.'; this.cdr.detectChanges(); })
    });
  }
}
