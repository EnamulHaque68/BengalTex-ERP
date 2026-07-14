import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { BudgetService } from '../../../services/budget.service';
import { AccountingService, CostCenterDto, FinancialYearDto } from '../../../services/accounting.service';
import { AccountDto } from '../../../models/accounting.models';
import { AuthService } from '../../../services/auth.service';
import { BudgetDto, BudgetLineInput, BudgetVarianceReportDto } from '../../../models/budget.models';
import { apiErrorMessage } from '../../../shared/utils/http-error.util';

interface EditLine {
  accountId: number | null;
  costCenterId: number | null;
  m: number[];   // 12 monthly amounts
}

/**
 * Phase A7a — annual budgets + Budget-vs-Actual variance. Budgets hold 12 FY-relative monthly
 * amounts per account (optionally per cost center); the variance report compares them to posted GL.
 */
@Component({
  selector: 'app-budgets',
  standalone: false,
  templateUrl: './budgets.component.html',
  styleUrl: './budgets.component.scss'
})
export class BudgetsComponent implements OnInit {
  loading = false;
  canManage = false;
  actionError = '';
  actionMessage = '';

  budgets: BudgetDto[] = [];
  financialYears: FinancialYearDto[] = [];
  accounts: AccountDto[] = [];
  costCenters: CostCenterDto[] = [];

  readonly monthNums = Array.from({ length: 12 }, (_, i) => i);

  // Create dialog
  createVisible = false;
  saving = false;
  createError = '';
  newFyId: number | null = null;
  newName = '';
  newNotes = '';

  // Edit lines dialog
  editVisible = false;
  editBudgetId: number | null = null;
  editBudgetCode = '';
  editStatus = '';
  editLines: EditLine[] = [];
  editSaving = false;
  editError = '';

  // Variance dialog
  varVisible = false;
  varBudget: BudgetDto | null = null;
  varFrom = 1;
  varTo = 12;
  varCcId: number | null = null;
  varReport: BudgetVarianceReportDto | null = null;
  varLoading = false;

  constructor(
    private svc: BudgetService,
    private accSvc: AccountingService,
    private auth: AuthService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('Accounting.CloseBooks');
    this.accSvc.getFinancialYears().subscribe({ next: (r) => this.zone.run(() => { if (r.success && r.data) this.financialYears = r.data; this.cdr.detectChanges(); }) });
    this.accSvc.getAccounts(undefined, false, true).subscribe({ next: (r) => this.zone.run(() => { if (r.success && r.data) this.accounts = r.data; this.cdr.detectChanges(); }) });
    this.accSvc.getCostCenters(false).subscribe({ next: (r) => this.zone.run(() => { if (r.success && r.data) this.costCenters = r.data; this.cdr.detectChanges(); }) });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getAll().subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.budgets = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  // ── Create ──
  openCreate(): void {
    this.newFyId = this.financialYears[0]?.id ?? null; this.newName = ''; this.newNotes = ''; this.createError = '';
    this.createVisible = true;
  }
  doCreate(): void {
    if (this.saving || !this.newFyId || !this.newName.trim()) return;
    this.saving = true; this.createError = '';
    this.svc.create({ financialYearId: this.newFyId, name: this.newName.trim(), notes: this.newNotes.trim() || null }).subscribe({
      next: (res) => this.zone.run(() => {
        this.saving = false;
        if (res.success) { this.createVisible = false; this.actionMessage = 'Budget created.'; this.load(); if (res.data) this.openEdit(res.data); }
        else this.createError = res.message || 'Create failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.saving = false; this.createError = apiErrorMessage(err, 'Create failed.'); this.cdr.detectChanges(); })
    });
  }

  // ── Edit lines ──
  openEdit(id: number): void {
    this.editVisible = true; this.editBudgetId = id; this.editLines = []; this.editError = ''; this.editBudgetCode = ''; this.editStatus = '';
    this.svc.getById(id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          this.editBudgetCode = res.data.code; this.editStatus = res.data.status;
          this.editLines = res.data.lines.map(l => ({
            accountId: l.accountId, costCenterId: l.costCenterId,
            m: [l.m1, l.m2, l.m3, l.m4, l.m5, l.m6, l.m7, l.m8, l.m9, l.m10, l.m11, l.m12]
          }));
        }
        this.cdr.detectChanges();
      })
    });
  }
  addLine(): void { this.editLines = [...this.editLines, { accountId: null, costCenterId: null, m: Array(12).fill(0) }]; }
  removeLine(i: number): void { this.editLines = this.editLines.filter((_, idx) => idx !== i); }
  lineTotal(l: EditLine): number { return l.m.reduce((a, b) => a + (Number(b) || 0), 0); }

  saveLines(): void {
    if (!this.editBudgetId || this.editSaving) return;
    const valid = this.editLines.filter(l => l.accountId);
    this.editSaving = true; this.editError = '';
    const payload: BudgetLineInput[] = valid.map(l => ({
      accountId: l.accountId!, costCenterId: l.costCenterId,
      m1: +l.m[0] || 0, m2: +l.m[1] || 0, m3: +l.m[2] || 0, m4: +l.m[3] || 0, m5: +l.m[4] || 0, m6: +l.m[5] || 0,
      m7: +l.m[6] || 0, m8: +l.m[7] || 0, m9: +l.m[8] || 0, m10: +l.m[9] || 0, m11: +l.m[10] || 0, m12: +l.m[11] || 0
    }));
    this.svc.setLines(this.editBudgetId, payload).subscribe({
      next: (res) => this.zone.run(() => {
        this.editSaving = false;
        if (res.success) { this.editVisible = false; this.actionMessage = res.message || 'Lines saved.'; this.load(); }
        else this.editError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.editSaving = false; this.editError = apiErrorMessage(err, 'Save failed.'); this.cdr.detectChanges(); })
    });
  }

  approve(b: BudgetDto): void {
    this.svc.approve(b.id).subscribe({
      next: (res) => this.zone.run(() => { if (res.success) { this.actionMessage = 'Budget approved.'; this.load(); } else this.actionError = res.message || 'Approve failed.'; this.cdr.detectChanges(); }),
      error: (err) => this.zone.run(() => { this.actionError = apiErrorMessage(err, 'Approve failed.'); this.cdr.detectChanges(); })
    });
  }
  deleteBudget(b: BudgetDto): void {
    this.svc.delete(b.id).subscribe({
      next: (res) => this.zone.run(() => { if (res.success) { this.actionMessage = 'Budget deleted.'; this.load(); } else this.actionError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (err) => this.zone.run(() => { this.actionError = apiErrorMessage(err, 'Delete failed.'); this.cdr.detectChanges(); })
    });
  }

  // ── Variance ──
  openVariance(b: BudgetDto): void {
    this.varBudget = b; this.varFrom = 1; this.varTo = 12; this.varCcId = null; this.varReport = null; this.varVisible = true;
    this.runVariance();
  }
  runVariance(): void {
    if (!this.varBudget) return;
    this.varLoading = true;
    this.svc.variance(this.varBudget.id, this.varFrom, this.varTo, this.varCcId ?? undefined).subscribe({
      next: (res) => this.zone.run(() => { this.varLoading = false; if (res.success && res.data) this.varReport = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.varLoading = false; this.cdr.detectChanges(); })
    });
  }

  statusClass(s: string): string { return s === 'Approved' ? 's-approved' : 's-draft'; }
  formatMoney(v: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 0 }).format(v || 0);
  }
}
