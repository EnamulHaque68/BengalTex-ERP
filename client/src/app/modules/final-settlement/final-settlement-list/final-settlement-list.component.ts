import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { FinalSettlementService } from '../../../services/final-settlement.service';
import { EmployeeService } from '../../../services/employee.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  FinalSettlementDto, FinalSettlementPreviewDto,
  FINAL_SETTLEMENT_STATUSES, SETTLEMENT_REASONS, SETTLEMENT_PAYMENT_METHODS
} from '../../../models/final-settlement.models';
import { EmployeeListItemDto } from '../../../models/employee.models';

@Component({
  selector: 'app-final-settlement-list',
  standalone: false,
  templateUrl: './final-settlement-list.component.html',
  styleUrl: './final-settlement-list.component.scss'
})
export class FinalSettlementListComponent implements OnInit {

  settlements: FinalSettlementDto[] = [];
  loading = false;
  totalCount = 0;
  actionError = '';
  actionMessage = '';
  rowActionId: number | null = null;

  readonly statuses = FINAL_SETTLEMENT_STATUSES;
  readonly reasons = SETTLEMENT_REASONS;
  readonly paymentMethods = SETTLEMENT_PAYMENT_METHODS;
  employees: EmployeeListItemDto[] = [];

  filterStatus: string | null = null;
  filterEmployeeId: number | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Create dialog
  createVisible = false;
  createSaving = false;
  createError = '';
  form!: FormGroup;
  preview: FinalSettlementPreviewDto | null = null;
  calculating = false;

  // View dialog
  viewVisible = false;
  viewing: FinalSettlementDto | null = null;

  // Mark-paid dialog
  payVisible = false;
  paying = false;
  payError = '';
  payTarget: FinalSettlementDto | null = null;
  payForm!: FormGroup;

  // Cancel dialog
  cancelVisible = false;
  cancelling = false;
  cancelError = '';
  cancelTarget: FinalSettlementDto | null = null;

  // Bank advice
  bankAdviceVisible = false;
  bankAdviceYear: number;
  bankAdviceMonth: number;
  bankAdviceLoading = false;
  bankAdviceError = '';

  constructor(
    private service: FinalSettlementService,
    private employeeService: EmployeeService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {
    const now = new Date();
    this.bankAdviceYear = now.getFullYear();
    this.bankAdviceMonth = now.getMonth() + 1;
  }

  ngOnInit(): void {
    this.form = this.fb.group({
      employeeId: [null, Validators.required],
      lastWorkingDate: [this.todayIso(), Validators.required],
      settlementDate: [this.todayIso(), Validators.required],
      reason: ['Resignation', Validators.required],
      proratedDays: [0, [Validators.required, Validators.min(0), Validators.max(31)]],
      proratedSalary: [0, [Validators.required, Validators.min(0)]],
      leaveEncashmentDays: [0, [Validators.required, Validators.min(0)]],
      leaveEncashmentAmount: [0, [Validators.required, Validators.min(0)]],
      gratuityAmount: [0, [Validators.required, Validators.min(0)]],
      otherEarnings: [0, [Validators.required, Validators.min(0)]],
      outstandingLoan: [0, [Validators.required, Validators.min(0)]],
      otherDeductions: [0, [Validators.required, Validators.min(0)]],
      notes: ['', Validators.maxLength(2000)]
    });

    this.payForm = this.fb.group({
      paymentMethod: ['BankTransfer', Validators.required],
      paymentReference: ['', Validators.maxLength(100)]
    });

    this.loadEmployees();
    this.load();
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency', currency: 'BDT', maximumFractionDigits: 2
    }).format(amount || 0);
  }

  statusBadgeClass(s: string): string {
    switch (s) {
      case 'Draft': return 'draft';
      case 'Approved': return 'approved';
      case 'Paid': return 'paid';
      case 'Cancelled': return 'cancelled';
      default: return '';
    }
  }

  reasonLabel(r: string): string {
    return this.reasons.find(x => x.value === r)?.label ?? r;
  }

  paymentMethodLabel(m: string | null): string {
    if (!m) return '—';
    return this.paymentMethods.find(x => x.value === m)?.label ?? m;
  }

  private todayIso(): string {
    return new Date().toISOString().substring(0, 10);
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
      this.filterStatus ?? undefined,
      this.filterEmployeeId ?? undefined
    ).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.settlements = res.data.items;
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

  // ─── Create ────────────────────────────────────────────────────────────────

  openCreate(): void {
    this.preview = null;
    this.createError = '';
    this.form.reset({
      employeeId: null,
      lastWorkingDate: this.todayIso(),
      settlementDate: this.todayIso(),
      reason: 'Resignation',
      proratedDays: 0, proratedSalary: 0,
      leaveEncashmentDays: 0, leaveEncashmentAmount: 0,
      gratuityAmount: 0, otherEarnings: 0,
      outstandingLoan: 0, otherDeductions: 0,
      notes: ''
    });
    this.createVisible = true;
  }

  /** Hit the calculate endpoint and fill the form with the preview values. */
  recalculate(): void {
    const empId = Number(this.form.get('employeeId')?.value);
    const lwd = this.form.get('lastWorkingDate')?.value as string;
    if (!empId || !lwd) return;
    this.calculating = true;
    this.createError = '';
    this.cdr.detectChanges();
    this.service.calculate(empId, lwd).subscribe({
      next: (res) => this.zone.run(() => {
        this.calculating = false;
        if (res.success && res.data) {
          this.preview = res.data;
          this.form.patchValue({
            proratedDays: res.data.proratedDays,
            proratedSalary: res.data.proratedSalary,
            leaveEncashmentDays: res.data.leaveEncashmentDays,
            leaveEncashmentAmount: res.data.leaveEncashmentAmount,
            gratuityAmount: res.data.gratuityAmount,
            outstandingLoan: res.data.outstandingLoan
          });
        } else {
          this.createError = res.message || 'Calculation failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.calculating = false;
        this.createError = err?.error?.message || 'Calculation failed.';
        this.cdr.detectChanges();
      })
    });
  }

  get previewGross(): number {
    const v = this.form?.getRawValue() ?? {};
    return (Number(v.proratedSalary) || 0) + (Number(v.leaveEncashmentAmount) || 0)
         + (Number(v.gratuityAmount) || 0) + (Number(v.otherEarnings) || 0);
  }

  get previewTotalDeductions(): number {
    const v = this.form?.getRawValue() ?? {};
    return (Number(v.outstandingLoan) || 0) + (Number(v.otherDeductions) || 0);
  }

  get previewNet(): number {
    return this.previewGross - this.previewTotalDeductions;
  }

  save(): void {
    if (this.form.invalid || this.createSaving) return;
    this.createSaving = true;
    this.createError = '';
    this.cdr.detectChanges();
    const v = this.form.getRawValue();
    this.service.create({
      employeeId: Number(v.employeeId),
      lastWorkingDate: v.lastWorkingDate,
      settlementDate: v.settlementDate,
      reason: v.reason,
      proratedDays: Number(v.proratedDays) || 0,
      proratedSalary: Number(v.proratedSalary) || 0,
      leaveEncashmentDays: Number(v.leaveEncashmentDays) || 0,
      leaveEncashmentAmount: Number(v.leaveEncashmentAmount) || 0,
      gratuityAmount: Number(v.gratuityAmount) || 0,
      otherEarnings: Number(v.otherEarnings) || 0,
      outstandingLoan: Number(v.outstandingLoan) || 0,
      otherDeductions: Number(v.otherDeductions) || 0,
      notes: (v.notes as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.createSaving = false;
        if (res.success) {
          this.createVisible = false;
          this.actionMessage = res.message || 'Settlement created.';
          this.load();
        } else {
          this.createError = res.message || 'Save failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.createSaving = false;
        this.createError = err?.error?.message || 'Save failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ─── View ──────────────────────────────────────────────────────────────────

  openView(s: FinalSettlementDto): void {
    this.viewing = s;
    this.viewVisible = true;
  }

  // ─── Approve ───────────────────────────────────────────────────────────────

  approve(s: FinalSettlementDto): void {
    if (this.rowActionId) return;
    if (!confirm(`Approve settlement ${s.code}? Employee will be marked inactive.`)) return;
    this.rowActionId = s.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.service.approve(s.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) { this.actionMessage = res.message || 'Approved.'; this.load(); }
        else this.actionError = res.message || 'Approve failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.rowActionId = null;
        this.actionError = err?.error?.message || 'Approve failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ─── Mark Paid ─────────────────────────────────────────────────────────────

  openMarkPaid(s: FinalSettlementDto): void {
    this.payTarget = s;
    this.payError = '';
    this.payForm.reset({ paymentMethod: 'BankTransfer', paymentReference: '' });
    this.payVisible = true;
  }

  doMarkPaid(): void {
    if (!this.payTarget || this.payForm.invalid || this.paying) return;
    this.paying = true;
    this.payError = '';
    this.cdr.detectChanges();
    const v = this.payForm.getRawValue();
    this.service.markPaid(this.payTarget.id, {
      paymentMethod: v.paymentMethod,
      paymentReference: (v.paymentReference as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.paying = false;
        if (res.success) {
          this.payVisible = false;
          this.actionMessage = res.message || 'Paid.';
          this.load();
        } else {
          this.payError = res.message || 'Action failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.paying = false;
        this.payError = err?.error?.message || 'Action failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ─── Cancel ────────────────────────────────────────────────────────────────

  openCancel(s: FinalSettlementDto): void {
    this.cancelTarget = s;
    this.cancelError = '';
    this.cancelVisible = true;
  }

  doCancel(): void {
    if (!this.cancelTarget || this.cancelling) return;
    this.cancelling = true;
    this.cancelError = '';
    this.cdr.detectChanges();
    this.service.cancel(this.cancelTarget.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.cancelling = false;
        if (res.success) {
          this.cancelVisible = false;
          this.actionMessage = res.message || 'Cancelled.';
          this.load();
        } else {
          this.cancelError = res.message || 'Cancel failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.cancelling = false;
        this.cancelError = err?.error?.message || 'Cancel failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ─── Bank-advice CSV download ──────────────────────────────────────────────

  openBankAdvice(): void {
    this.bankAdviceError = '';
    this.bankAdviceVisible = true;
  }

  downloadBankAdvice(): void {
    if (this.bankAdviceLoading) return;
    this.bankAdviceLoading = true;
    this.bankAdviceError = '';
    this.cdr.detectChanges();
    this.service.downloadBankAdvice(this.bankAdviceYear, this.bankAdviceMonth).subscribe({
      next: (blob) => this.zone.run(() => {
        this.bankAdviceLoading = false;
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        const yy = this.bankAdviceYear.toString().padStart(4, '0');
        const mm = this.bankAdviceMonth.toString().padStart(2, '0');
        a.download = `BankAdvice-${yy}-${mm}.csv`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.bankAdviceVisible = false;
        this.actionMessage = `Bank advice for ${yy}-${mm} downloaded.`;
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.bankAdviceLoading = false;
        this.bankAdviceError = err?.error?.message || 'Download failed.';
        this.cdr.detectChanges();
      })
    });
  }
}
