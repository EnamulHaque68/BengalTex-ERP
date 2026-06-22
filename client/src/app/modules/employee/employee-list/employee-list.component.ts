import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { EmployeeService } from '../../../services/employee.service';
import { MasterSetupService } from '../../../services/master-setup.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  EmployeeListItemDto, GENDERS, EMPLOYMENT_TYPES, EMPLOYEE_STATUSES,
  EMPLOYEE_HISTORY_TYPES, EmployeeHistoryEntryDto, EmployeeLoginStatusDto
} from '../../../models/employee.models';
import {
  DepartmentDto, DesignationDto, ShiftDto, BankAccountDto
} from '../../../models/master-setup.models';

@Component({
  selector: 'app-employee-list',
  standalone: false,
  templateUrl: './employee-list.component.html',
  styleUrl: './employee-list.component.scss'
})
export class EmployeeListComponent implements OnInit {

  employees: EmployeeListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  includeInactive = false;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly genders = GENDERS;
  readonly employmentTypes = EMPLOYMENT_TYPES;
  readonly statuses = EMPLOYEE_STATUSES;

  // Master-Setup dropdowns
  departments: DepartmentDto[] = [];
  designations: DesignationDto[] = [];
  shifts: ShiftDto[] = [];
  bankAccounts: BankAccountDto[] = [];

  // Supervisor (Reporting To) picker — all employees, minus the one being edited
  allEmployees: EmployeeListItemDto[] = [];

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingEmployee: EmployeeListItemDto | null = null;
  deleting = false;
  deleteError = '';

  // Manage Login
  loginVisible = false;
  loginEmployee: EmployeeListItemDto | null = null;
  loginLoading = false;
  loginBusy = false;
  loginError = '';
  loginInfo = '';
  loginStatus: EmployeeLoginStatusDto | null = null;
  newUserName = '';
  newEmail = '';
  newPassword = '';
  resetPassword = '';

  // Service-record history
  readonly historyTypes = EMPLOYEE_HISTORY_TYPES;
  historyVisible = false;
  historyEmployee: EmployeeListItemDto | null = null;
  history: EmployeeHistoryEntryDto[] = [];
  historyLoading = false;
  historySaving = false;
  historyError = '';
  historyForm!: FormGroup;

  constructor(
    private service: EmployeeService,
    private masterSvc: MasterSetupService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.historyForm = this.fb.group({
      type: ['Increment', Validators.required],
      effectiveDate: [new Date().toISOString().slice(0, 10), Validators.required],
      title: ['', [Validators.required, Validators.maxLength(200)]],
      fromValue: ['', Validators.maxLength(200)],
      toValue: ['', Validators.maxLength(200)],
      amount: [null as number | null],
      details: ['', Validators.maxLength(2000)]
    });
    this.loadMasters();
    this.load();
  }

  // ── Service-record history ──
  openHistory(emp: EmployeeListItemDto): void {
    this.historyEmployee = emp;
    this.history = [];
    this.historyError = '';
    this.historyForm.reset({ type: 'Increment', effectiveDate: new Date().toISOString().slice(0, 10), title: '', fromValue: '', toValue: '', amount: null, details: '' });
    this.historyVisible = true;
    this.loadHistory();
  }

  private loadHistory(): void {
    if (!this.historyEmployee) return;
    this.historyLoading = true;
    this.cdr.detectChanges();
    this.service.getHistory(this.historyEmployee.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.historyLoading = false;
        if (res.success && res.data) this.history = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.historyLoading = false; this.cdr.detectChanges(); })
    });
  }

  addHistoryEntry(): void {
    if (this.historyForm.invalid || this.historySaving || !this.historyEmployee) return;
    this.historySaving = true;
    this.historyError = '';
    this.cdr.detectChanges();
    const v = this.historyForm.getRawValue();
    this.service.addHistory(this.historyEmployee.id, {
      type: v.type, effectiveDate: v.effectiveDate, title: (v.title as string).trim(),
      fromValue: (v.fromValue as string)?.trim() || null, toValue: (v.toValue as string)?.trim() || null,
      amount: v.amount != null ? Number(v.amount) : null, details: (v.details as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.historySaving = false;
        if (res.success) {
          this.historyForm.patchValue({ title: '', fromValue: '', toValue: '', amount: null, details: '' });
          this.loadHistory();
        } else this.historyError = res.message || 'Could not add entry.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.historySaving = false; this.historyError = err?.error?.message || 'Could not add entry.'; this.cdr.detectChanges(); })
    });
  }

  deleteHistoryEntry(h: EmployeeHistoryEntryDto): void {
    this.service.deleteHistory(h.id).subscribe({
      next: () => this.zone.run(() => { this.loadHistory(); }),
      error: () => {}
    });
  }

  private loadMasters(): void {
    this.masterSvc.getDepartments(false).subscribe({ next: (res) => this.zone.run(() => { if (res.success && res.data) this.departments = res.data; this.cdr.detectChanges(); }) });
    this.masterSvc.getDesignations(false).subscribe({ next: (res) => this.zone.run(() => { if (res.success && res.data) this.designations = res.data; this.cdr.detectChanges(); }) });
    this.masterSvc.getShifts(false).subscribe({ next: (res) => this.zone.run(() => { if (res.success && res.data) this.shifts = res.data; this.cdr.detectChanges(); }) });
    this.masterSvc.getBankAccounts(false).subscribe({ next: (res) => this.zone.run(() => { if (res.success && res.data) this.bankAccounts = res.data; this.cdr.detectChanges(); }) });
    this.service.getAll({ page: 1, pageSize: 1000, search: '' }).subscribe({ next: (res) => this.zone.run(() => { if (res.success && res.data) this.allEmployees = res.data.items; this.cdr.detectChanges(); }) });
  }

  /** Supervisor options for the form — exclude the employee currently being edited. */
  get supervisorOptions(): EmployeeListItemDto[] {
    return this.editingId ? this.allEmployees.filter(e => e.id !== this.editingId) : this.allEmployees;
  }

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private buildForm(): void {
    this.form = this.fb.group({
      code: [''],
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      designation: ['', Validators.maxLength(100)],
      department: ['', Validators.maxLength(100)],
      phone: ['', Validators.maxLength(30)],
      email: ['', Validators.maxLength(200)],
      nationalId: ['', Validators.maxLength(50)],
      address: ['', Validators.maxLength(500)],
      joiningDate: [this.todayIso(), Validators.required],
      dateOfBirth: [null as string | null],
      gender: ['Male', Validators.required],
      employmentType: ['Permanent', Validators.required],
      basicSalary: [0, [Validators.required, Validators.min(0)]],
      houseRentAllowance: [0, [Validators.min(0)]],
      medicalAllowance: [0, [Validators.min(0)]],
      transportAllowance: [0, [Validators.min(0)]],
      foodAllowance: [0, [Validators.min(0)]],
      isPfMember: [false],
      pfRate: [10, [Validators.min(0), Validators.max(100)]],
      isTaxable: [false],
      departmentId: [null as number | null],
      designationId: [null as number | null],
      shiftId: [null as number | null],
      bankAccountId: [null as number | null],
      reportingToEmployeeId: [null as number | null],
      status: ['Active'],
      notes: ['', Validators.maxLength(2000)],
      isActive: [true]
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency', currency: 'BDT', maximumFractionDigits: 2
    }).format(amount || 0);
  }

  // ─── Data loading ──────────────────────────────────────────────────────────

  load(): void {
    this.loading = true;
    this.service.getAll(this.parameters, this.includeInactive, undefined, this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.employees = res.data.items;
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

  // ─── Create / Edit dialog ──────────────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.reset({
      code: '',
      fullName: '',
      designation: '',
      department: '',
      phone: '',
      email: '',
      nationalId: '',
      address: '',
      joiningDate: this.todayIso(),
      dateOfBirth: null,
      gender: 'Male',
      employmentType: 'Permanent',
      basicSalary: 0,
      houseRentAllowance: 0,
      medicalAllowance: 0,
      transportAllowance: 0,
      foodAllowance: 0,
      isPfMember: false,
      pfRate: 10,
      isTaxable: false,
      departmentId: null,
      designationId: null,
      shiftId: null,
      bankAccountId: null,
      reportingToEmployeeId: null,
      status: 'Active',
      notes: '',
      isActive: true
    });
    this.dialogVisible = true;
  }

  openEdit(emp: EmployeeListItemDto): void {
    this.dialogMode = 'edit';
    this.editingId = emp.id;
    this.dialogError = '';
    this.dialogVisible = true;

    this.service.getById(emp.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const e = res.data;
          this.form.patchValue({
            code: e.code,
            fullName: e.fullName,
            designation: e.designation ?? '',
            department: e.department ?? '',
            phone: e.phone ?? '',
            email: e.email ?? '',
            nationalId: e.nationalId ?? '',
            address: e.address ?? '',
            joiningDate: e.joiningDate,
            dateOfBirth: e.dateOfBirth ?? null,
            gender: e.gender,
            employmentType: e.employmentType,
            basicSalary: e.basicSalary,
            houseRentAllowance: e.houseRentAllowance,
            medicalAllowance: e.medicalAllowance,
            transportAllowance: e.transportAllowance,
            foodAllowance: e.foodAllowance,
            isPfMember: e.isPfMember,
            pfRate: e.pfRate,
            isTaxable: e.isTaxable,
            departmentId: e.departmentId,
            designationId: e.designationId,
            shiftId: e.shiftId,
            bankAccountId: e.bankAccountId,
            reportingToEmployeeId: e.reportingToEmployeeId,
            status: e.status,
            notes: e.notes ?? '',
            isActive: e.isActive
          });
          this.cdr.detectChanges();
        }
      })
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();
    const common = {
      fullName: v.fullName,
      designation: (v.designation as string)?.trim() || null,
      department: (v.department as string)?.trim() || null,
      phone: (v.phone as string)?.trim() || null,
      email: (v.email as string)?.trim() || null,
      nationalId: (v.nationalId as string)?.trim() || null,
      address: (v.address as string)?.trim() || null,
      joiningDate: v.joiningDate,
      dateOfBirth: v.dateOfBirth || null,
      gender: v.gender,
      employmentType: v.employmentType,
      basicSalary: Number(v.basicSalary) || 0,
      houseRentAllowance: Number(v.houseRentAllowance) || 0,
      medicalAllowance: Number(v.medicalAllowance) || 0,
      transportAllowance: Number(v.transportAllowance) || 0,
      foodAllowance: Number(v.foodAllowance) || 0,
      isPfMember: !!v.isPfMember,
      pfRate: Number(v.pfRate) || 0,
      isTaxable: !!v.isTaxable,
      departmentId: v.departmentId,
      designationId: v.designationId,
      shiftId: v.shiftId,
      bankAccountId: v.bankAccountId,
      reportingToEmployeeId: v.reportingToEmployeeId ?? null,
      notes: (v.notes as string)?.trim() || null
    };

    if (this.dialogMode === 'create') {
      this.service.create({ ...common, code: (v.code as string)?.trim() || null }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.service.update(this.editingId, { ...common, status: v.status, isActive: v.isActive }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    }
  }

  private handleSave(res: { success: boolean; message?: string | null }): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      if (res.success) {
        this.dialogVisible = false;
        this.load();
      } else {
        this.dialogError = res.message || 'Save failed.';
      }
      this.cdr.detectChanges();
    });
  }

  private handleError(err: any): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      this.dialogError = err?.error?.message || 'Save failed.';
      this.cdr.detectChanges();
    });
  }

  // ─── Manage Login ──────────────────────────────────────────────────────────

  openLogin(emp: EmployeeListItemDto): void {
    this.loginEmployee = emp;
    this.loginVisible = true;
    this.loginStatus = null;
    this.loginError = ''; this.loginInfo = '';
    this.newUserName = ''; this.newEmail = ''; this.newPassword = ''; this.resetPassword = '';
    this.loadLoginStatus();
  }

  private loadLoginStatus(): void {
    if (!this.loginEmployee) return;
    this.loginLoading = true; this.cdr.detectChanges();
    this.service.getLoginStatus(this.loginEmployee.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.loginLoading = false;
        if (res.success && res.data) {
          this.loginStatus = res.data;
          this.newUserName = res.data.suggestedUserName;
          this.newEmail = res.data.employeeEmail ?? '';
        } else this.loginError = res.message || 'Could not load login status.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.loginLoading = false; this.loginError = err?.error?.message || 'Could not load login status.'; this.cdr.detectChanges(); })
    });
  }

  createLogin(): void {
    if (!this.loginEmployee || this.loginBusy) return;
    if (!this.newUserName.trim() || this.newPassword.length < 8) {
      this.loginError = 'Username required and password must be at least 8 characters.'; this.cdr.detectChanges(); return;
    }
    this.loginBusy = true; this.loginError = ''; this.loginInfo = ''; this.cdr.detectChanges();
    this.service.createLogin(this.loginEmployee.id, {
      userName: this.newUserName.trim(), password: this.newPassword,
      roleName: this.loginStatus?.designationAccessRoleName ?? null,
      email: this.newEmail.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.loginBusy = false;
        if (res.success && res.data) { this.loginStatus = res.data; this.newPassword = ''; this.loginInfo = 'Login account created.'; }
        else this.loginError = res.message || 'Could not create login.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.loginBusy = false; this.loginError = err?.error?.message || 'Could not create login.'; this.cdr.detectChanges(); })
    });
  }

  doResetPassword(): void {
    if (!this.loginEmployee || this.loginBusy) return;
    if (this.resetPassword.length < 8) { this.loginError = 'Password must be at least 8 characters.'; this.cdr.detectChanges(); return; }
    this.loginBusy = true; this.loginError = ''; this.loginInfo = ''; this.cdr.detectChanges();
    this.service.resetLoginPassword(this.loginEmployee.id, this.resetPassword).subscribe({
      next: (res) => this.zone.run(() => {
        this.loginBusy = false;
        if (res.success) { this.resetPassword = ''; this.loginInfo = 'Password reset.'; } else this.loginError = res.message || 'Could not reset password.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.loginBusy = false; this.loginError = err?.error?.message || 'Could not reset password.'; this.cdr.detectChanges(); })
    });
  }

  syncRole(): void {
    if (!this.loginEmployee || this.loginBusy) return;
    this.loginBusy = true; this.loginError = ''; this.loginInfo = ''; this.cdr.detectChanges();
    this.service.setLoginRole(this.loginEmployee.id, this.loginStatus?.designationAccessRoleName ?? null).subscribe({
      next: (res) => this.zone.run(() => {
        this.loginBusy = false;
        if (res.success && res.data) { this.loginStatus = res.data; this.loginInfo = 'Access synced to designation.'; } else this.loginError = res.message || 'Could not sync access.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.loginBusy = false; this.loginError = err?.error?.message || 'Could not sync access.'; this.cdr.detectChanges(); })
    });
  }

  unlinkLogin(deactivate: boolean): void {
    if (!this.loginEmployee || this.loginBusy) return;
    if (!confirm(deactivate ? 'Unlink AND deactivate this login?' : 'Unlink this login from the employee?')) return;
    this.loginBusy = true; this.loginError = ''; this.loginInfo = ''; this.cdr.detectChanges();
    this.service.unlinkLogin(this.loginEmployee.id, deactivate).subscribe({
      next: (res) => this.zone.run(() => {
        this.loginBusy = false;
        if (res.success) { this.loginInfo = 'Login unlinked.'; this.loadLoginStatus(); } else this.loginError = res.message || 'Could not unlink.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.loginBusy = false; this.loginError = err?.error?.message || 'Could not unlink.'; this.cdr.detectChanges(); })
    });
  }

  // ─── Delete ────────────────────────────────────────────────────────────────

  confirmDelete(emp: EmployeeListItemDto): void {
    this.deletingEmployee = emp;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingEmployee || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.service.delete(this.deletingEmployee.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) {
          this.deleteDialogVisible = false;
          this.deletingEmployee = null;
          this.load();
        } else {
          this.deleteError = res.message || 'Delete failed.';
        }
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
