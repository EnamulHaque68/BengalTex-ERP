import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { BankReconciliationService } from '../../../services/bank-reconciliation.service';
import { MasterSetupService } from '../../../services/master-setup.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { BankStatementListItemDto } from '../../../models/bank-reconciliation.models';
import { BankAccountDto } from '../../../models/master-setup.models';

@Component({
  selector: 'app-bank-statement-list',
  standalone: false,
  templateUrl: './bank-statement-list.component.html',
  styleUrl: './bank-statement-list.component.scss'
})
export class BankStatementListComponent implements OnInit {
  statements: BankStatementListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterBankAccountId: number | null = null;
  filterStatus: 'all' | 'reconciled' | 'unreconciled' = 'unreconciled';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  bankAccounts: BankAccountDto[] = [];

  canManage = false;
  dialogVisible = false;
  dialogSaving = false;
  dialogError = '';
  form!: FormGroup;

  constructor(
    private svc: BankReconciliationService,
    private masterSvc: MasterSetupService,
    private auth: AuthService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('BankReconciliation.Manage');
    this.form = this.fb.group({
      bankAccountId: [null as number | null, Validators.required],
      statementDate: [this.todayIso(), Validators.required],
      periodFromDate: [this.monthStartIso(), Validators.required],
      periodToDate: [this.todayIso(), Validators.required],
      openingBalance: [0, Validators.required],
      closingBalance: [0, Validators.required],
      notes: ['', Validators.maxLength(2000)]
    });
    this.masterSvc.getBankAccounts(false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.bankAccounts = res.data; this.cdr.detectChanges(); })
    });
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }
  private monthStartIso(): string {
    const d = new Date(); return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
  }

  get isReconciledFilter(): boolean | null {
    if (this.filterStatus === 'reconciled') return true;
    if (this.filterStatus === 'unreconciled') return false;
    return null;
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters, this.filterBankAccountId ?? undefined, this.isReconciledFilter).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.statements = res.data.items; this.totalCount = res.data.totalCount; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearch(v: string): void { if (this.searchTimer) clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => { this.parameters.search = v; this.parameters.page = 1; this.load(); }, 350); }
  onPage(e: any): void { this.parameters.page = Math.floor(e.first / e.rows) + 1; this.parameters.pageSize = e.rows; this.load(); }

  openCreate(): void {
    this.dialogError = '';
    this.form.reset({
      bankAccountId: null, statementDate: this.todayIso(),
      periodFromDate: this.monthStartIso(), periodToDate: this.todayIso(),
      openingBalance: 0, closingBalance: 0, notes: ''
    });
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    this.svc.create({
      bankAccountId: v.bankAccountId, statementDate: v.statementDate,
      periodFromDate: v.periodFromDate, periodToDate: v.periodToDate,
      openingBalance: Number(v.openingBalance) || 0, closingBalance: Number(v.closingBalance) || 0,
      notes: (v.notes as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.dialogSaving = false;
        if (res.success) {
          this.dialogVisible = false;
          if (res.data) this.router.navigate(['/bank-reconciliation', res.data]);
          else this.load();
        } else this.dialogError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); })
    });
  }

  open(s: BankStatementListItemDto): void { this.router.navigate(['/bank-reconciliation', s.id]); }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }

  progressPercent(s: BankStatementListItemDto): number {
    if (s.lineCount === 0) return 0;
    return Math.round((s.matchedCount / s.lineCount) * 100);
  }
}
