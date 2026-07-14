import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LoanBonusService } from '../../../services/loan-bonus.service';
import { EmployeeService } from '../../../services/employee.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { EmployeeLoanDto, EmployeeLoanStatus, LOAN_STATUSES, DISBURSEMENT_METHODS } from '../../../models/loan-bonus.models';

@Component({
  selector: 'app-employee-loan-list',
  standalone: false,
  templateUrl: './employee-loan-list.component.html',
  styleUrl: './employee-loan-list.component.scss'
})
export class EmployeeLoanListComponent implements OnInit {
  loans: EmployeeLoanDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: EmployeeLoanStatus | null = 'Active';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  employees: any[] = [];

  readonly statuses = LOAN_STATUSES;
  readonly disbursementMethods = DISBURSEMENT_METHODS;

  canCreate = false;
  canEdit = false;
  canClose = false;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  closeDialogVisible = false;
  closeTarget: EmployeeLoanDto | null = null;
  closeAsCancel = false;
  closeBusy = false;
  closeError = '';

  constructor(
    private svc: LoanBonusService,
    private empService: EmployeeService,
    private auth: AuthService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canCreate = this.auth.hasPermission('EmployeeLoans.Create');
    this.canEdit = this.auth.hasPermission('EmployeeLoans.Edit');
    this.canClose = this.auth.hasPermission('EmployeeLoans.Close');

    this.form = this.fb.group({
      employeeId: [null as number | null, Validators.required],
      issuedDate: [this.todayIso(), Validators.required],
      principal: [0, [Validators.required, Validators.min(0.01)]],
      emiAmount: [0, [Validators.required, Validators.min(0.01)]],
      tenureMonths: [12, [Validators.required, Validators.min(1)]],
      startYearMonth: [this.currentYm(), [Validators.required]],
      disbursementMethod: ['BankTransfer', Validators.required],
      notes: ['', Validators.maxLength(1000)]
    });

    this.loadEmployees();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }
  private currentYm(): number { const d = new Date(); return d.getFullYear() * 100 + (d.getMonth() + 1); }

  private loadEmployees(): void {
    this.empService.getAll({ page: 1, pageSize: 1000, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.employees = res.data.items; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getLoans(this.parameters, this.filterStatus).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.loans = res.data.items; this.totalCount = res.data.totalCount; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearch(v: string): void { if (this.searchTimer) clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => { this.parameters.search = v; this.parameters.page = 1; this.load(); }, 350); }
  onPage(e: any): void { this.parameters.page = Math.floor(e.first / e.rows) + 1; this.parameters.pageSize = e.rows; this.load(); }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({ employeeId: null, issuedDate: this.todayIso(), principal: 0, emiAmount: 0, tenureMonths: 12, startYearMonth: this.currentYm(), disbursementMethod: 'BankTransfer', notes: '' });
    this.form.get('employeeId')?.enable(); this.form.get('issuedDate')?.enable(); this.form.get('principal')?.enable();
    this.dialogVisible = true;
  }
  openEdit(l: EmployeeLoanDto): void {
    this.dialogMode = 'edit'; this.editingId = l.id; this.dialogError = '';
    this.form.reset({
      employeeId: l.employeeId, issuedDate: l.issuedDate, principal: l.principal,
      emiAmount: l.emiAmount, tenureMonths: l.tenureMonths, startYearMonth: l.startYearMonth,
      notes: l.notes ?? ''
    });
    this.form.get('employeeId')?.disable(); this.form.get('issuedDate')?.disable(); this.form.get('principal')?.disable();
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const notes = (v.notes as string)?.trim() || null;
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') {
      this.svc.createLoan({
        employeeId: v.employeeId, issuedDate: v.issuedDate, principal: Number(v.principal) || 0,
        emiAmount: Number(v.emiAmount) || 0, tenureMonths: Number(v.tenureMonths) || 1,
        startYearMonth: Number(v.startYearMonth) || this.currentYm(),
        disbursementMethod: v.disbursementMethod || 'BankTransfer', notes
      }).subscribe({ next: done, error: err });
    } else {
      this.svc.updateLoan(this.editingId!, {
        emiAmount: Number(v.emiAmount) || 0, tenureMonths: Number(v.tenureMonths) || 1,
        startYearMonth: Number(v.startYearMonth) || this.currentYm(), notes
      }).subscribe({ next: done, error: err });
    }
  }

  openClose(l: EmployeeLoanDto, asCancel: boolean): void { this.closeTarget = l; this.closeAsCancel = asCancel; this.closeError = ''; this.closeDialogVisible = true; }
  doClose(): void {
    if (!this.closeTarget || this.closeBusy) return;
    this.closeBusy = true; this.closeError = ''; this.cdr.detectChanges();
    this.svc.closeLoan(this.closeTarget.id, this.closeAsCancel).subscribe({
      next: (res) => this.zone.run(() => { this.closeBusy = false; if (res.success) { this.closeDialogVisible = false; this.closeTarget = null; this.load(); } else this.closeError = res.message || 'Close failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.closeBusy = false; this.closeError = e?.error?.message || 'Close failed.'; this.cdr.detectChanges(); })
    });
  }

  statusSeverity(s: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    return s === 'Active' ? 'success' : s === 'Closed' ? 'info' : 'secondary';
  }

  progressPercent(l: EmployeeLoanDto): number {
    if (l.principal <= 0) return 0;
    return Math.min(100, Math.round((l.totalRepaid / l.principal) * 100));
  }

  formatYearMonth(ym: number): string {
    const y = Math.floor(ym / 100), m = ym % 100;
    return `${y}-${m.toString().padStart(2, '0')}`;
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }
}
