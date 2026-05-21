import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { EmployeeService } from '../../../services/employee.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  EmployeeListItemDto, GENDERS, EMPLOYMENT_TYPES, EMPLOYEE_STATUSES
} from '../../../models/employee.models';

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

  constructor(
    private service: EmployeeService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.load();
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
