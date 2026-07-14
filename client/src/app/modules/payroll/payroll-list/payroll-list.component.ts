import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PayrollService } from '../../../services/payroll.service';
import { EmployeeService } from '../../../services/employee.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { PayslipDto, PAYSLIP_STATUSES, MONTHS } from '../../../models/payroll.models';
import { EmployeeListItemDto } from '../../../models/employee.models';

@Component({
  selector: 'app-payroll-list',
  standalone: false,
  templateUrl: './payroll-list.component.html',
  styleUrl: './payroll-list.component.scss'
})
export class PayrollListComponent implements OnInit {

  payslips: PayslipDto[] = [];
  loading = false;
  totalCount = 0;
  actionError = '';
  actionMessage = '';

  readonly statuses = PAYSLIP_STATUSES;
  readonly months = MONTHS;
  employees: EmployeeListItemDto[] = [];

  filterYear: number;
  filterMonth: number | null;
  filterEmployeeId: number | null = null;
  filterStatus: string | null = null;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;
  rowActionId: number | null = null;

  // Generate dialog
  generateVisible = false;
  generateYear: number;
  generateMonth: number;
  generating = false;
  generateError = '';

  // Edit dialog
  editVisible = false;
  editSaving = false;
  editError = '';
  editing: PayslipDto | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deleting = false;
  deleteError = '';
  deletingPayslip: PayslipDto | null = null;

  constructor(
    private service: PayrollService,
    private employeeService: EmployeeService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {
    const now = new Date();
    this.filterYear = now.getFullYear();
    this.filterMonth = now.getMonth() + 1;
    this.generateYear = now.getFullYear();
    this.generateMonth = now.getMonth() + 1;
  }

  ngOnInit(): void {
    this.form = this.fb.group({
      overtimeAmount: [0, [Validators.required, Validators.min(0)]],
      allowances: [0, [Validators.required, Validators.min(0)]],
      deductions: [0, [Validators.required, Validators.min(0)]],
      houseRent: [0, [Validators.min(0)]],
      medical: [0, [Validators.min(0)]],
      transport: [0, [Validators.min(0)]],
      foodAllowance: [0, [Validators.min(0)]],
      festivalBonus: [0, [Validators.min(0)]],
      pfEmployee: [0, [Validators.min(0)]],
      pfEmployer: [0, [Validators.min(0)]],
      incomeTax: [0, [Validators.min(0)]],
      loanDeduction: [0, [Validators.min(0)]],
      notes: ['', Validators.maxLength(1000)]
    });
    this.loadEmployees();
    this.load();
  }

  monthName(m: number): string {
    return this.months.find(x => x.value === m)?.label ?? `${m}`;
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency', currency: 'BDT', maximumFractionDigits: 2
    }).format(amount || 0);
  }

  private loadEmployees(): void {
    this.employeeService.getAll({ page: 1, pageSize: 1000, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.employees = res.data.items;
        this.cdr.detectChanges();
      })
    });
  }

  // ─── Load ──────────────────────────────────────────────────────────────────

  load(): void {
    this.loading = true;
    this.service.getAll(
      this.parameters,
      this.filterYear || undefined,
      this.filterMonth ?? undefined,
      this.filterEmployeeId ?? undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.payslips = res.data.items;
          this.totalCount = res.data.totalCount;
        }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearchChange(value: string): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.parameters.search = value;
      this.parameters.page = 1;
      this.load();
    }, 400);
  }

  onPageChange(event: any): void {
    this.parameters.page = Math.floor(event.first / event.rows) + 1;
    this.parameters.pageSize = event.rows;
    this.load();
  }

  // ─── Generate ──────────────────────────────────────────────────────────────

  openGenerate(): void {
    this.generateYear = this.filterYear;
    this.generateMonth = this.filterMonth ?? (new Date().getMonth() + 1);
    this.generateError = '';
    this.generateVisible = true;
  }

  doGenerate(): void {
    if (this.generating) return;
    this.generating = true;
    this.generateError = '';
    this.cdr.detectChanges();
    this.service.generate({ year: this.generateYear, month: this.generateMonth }).subscribe({
      next: (res) => this.zone.run(() => {
        this.generating = false;
        if (res.success) {
          this.generateVisible = false;
          this.actionMessage = res.message || 'Payroll generated.';
          this.filterYear = this.generateYear;
          this.filterMonth = this.generateMonth;
          this.load();
        } else {
          this.generateError = res.message || 'Generation failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.generating = false;
        this.generateError = err?.error?.message || 'Generation failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ─── Edit (adjust) ─────────────────────────────────────────────────────────

  openEdit(p: PayslipDto): void {
    this.editing = p;
    this.editError = '';
    this.form.reset({
      overtimeAmount: p.overtimeAmount,
      allowances: p.allowances,
      // Deductions input on the form represents OTHER deductions only
      // (absence + adjustments). PF + IncomeTax + Loan add to it in the handler.
      deductions: Math.max(0, p.deductions - p.pfEmployee - p.incomeTax - p.loanDeduction),
      houseRent: p.houseRent,
      medical: p.medical,
      transport: p.transport,
      foodAllowance: p.foodAllowance,
      festivalBonus: p.festivalBonus,
      pfEmployee: p.pfEmployee,
      pfEmployer: p.pfEmployer,
      incomeTax: p.incomeTax,
      loanDeduction: p.loanDeduction,
      notes: p.notes ?? ''
    });
    this.editVisible = true;
  }

  get previewGross(): number {
    if (!this.editing) return 0;
    const v = this.form.getRawValue();
    return (this.editing.basicSalary || 0)
      + (Number(v.allowances) || 0) + (Number(v.overtimeAmount) || 0)
      + (Number(v.houseRent) || 0) + (Number(v.medical) || 0)
      + (Number(v.transport) || 0) + (Number(v.foodAllowance) || 0)
      + (Number(v.festivalBonus) || 0);
  }

  get previewTotalDeductions(): number {
    const v = this.form.getRawValue();
    return (Number(v.deductions) || 0) + (Number(v.pfEmployee) || 0)
      + (Number(v.incomeTax) || 0) + (Number(v.loanDeduction) || 0);
  }

  get previewNet(): number {
    return this.previewGross - this.previewTotalDeductions;
  }

  saveEdit(): void {
    if (!this.editing || this.form.invalid || this.editSaving) return;
    this.editSaving = true;
    this.editError = '';
    this.cdr.detectChanges();
    const v = this.form.getRawValue();
    this.service.update(this.editing.id, {
      overtimeAmount: Number(v.overtimeAmount) || 0,
      allowances: Number(v.allowances) || 0,
      deductions: Number(v.deductions) || 0,
      houseRent: Number(v.houseRent) || 0,
      medical: Number(v.medical) || 0,
      transport: Number(v.transport) || 0,
      foodAllowance: Number(v.foodAllowance) || 0,
      festivalBonus: Number(v.festivalBonus) || 0,
      pfEmployee: Number(v.pfEmployee) || 0,
      pfEmployer: Number(v.pfEmployer) || 0,
      incomeTax: Number(v.incomeTax) || 0,
      loanDeduction: Number(v.loanDeduction) || 0,
      notes: (v.notes as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.editSaving = false;
        if (res.success) { this.editVisible = false; this.load(); }
        else this.editError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.editSaving = false;
        this.editError = err?.error?.message || 'Save failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ─── Approve (accrue) ──────────────────────────────────────────────────────

  approve(p: PayslipDto): void {
    if (this.rowActionId) return;
    this.rowActionId = p.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.service.approve(p.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) { this.actionMessage = res.message || 'Payslip approved.'; this.load(); }
        else this.actionError = res.message || 'Approval failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.rowActionId = null;
        this.actionError = err?.error?.message || 'Approval failed.';
        this.cdr.detectChanges();
      })
    });
  }

  approveMonth(): void {
    if (this.rowActionId) return;
    const month = this.filterMonth ?? (new Date().getMonth() + 1);
    this.rowActionId = -1;
    this.actionError = '';
    this.cdr.detectChanges();
    this.service.approveMonth({ year: this.filterYear, month }).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) { this.actionMessage = res.message || 'Payslips approved.'; this.load(); }
        else this.actionError = res.message || 'Approval failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.rowActionId = null;
        this.actionError = err?.error?.message || 'Approval failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ─── Mark paid ─────────────────────────────────────────────────────────────

  markPaid(p: PayslipDto): void {
    if (this.rowActionId) return;
    this.rowActionId = p.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.service.markPaid(p.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) this.load();
        else this.actionError = res.message || 'Action failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.rowActionId = null;
        this.actionError = err?.error?.message || 'Action failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ─── Delete ────────────────────────────────────────────────────────────────

  confirmDelete(p: PayslipDto): void {
    this.deletingPayslip = p;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingPayslip || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();
    this.service.delete(this.deletingPayslip.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) { this.deleteDialogVisible = false; this.deletingPayslip = null; this.load(); }
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
}
