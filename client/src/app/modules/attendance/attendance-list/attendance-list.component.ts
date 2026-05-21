import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AttendanceService } from '../../../services/attendance.service';
import { EmployeeService } from '../../../services/employee.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { AttendanceRecordDto, ATTENDANCE_STATUSES } from '../../../models/attendance.models';
import { EmployeeListItemDto } from '../../../models/employee.models';

@Component({
  selector: 'app-attendance-list',
  standalone: false,
  templateUrl: './attendance-list.component.html',
  styleUrl: './attendance-list.component.scss'
})
export class AttendanceListComponent implements OnInit {

  records: AttendanceRecordDto[] = [];
  loading = false;
  totalCount = 0;
  actionError = '';

  filterFromDate: string;
  filterToDate: string;
  filterEmployeeId: number | null = null;
  filterStatus: string | null = null;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = ATTENDANCE_STATUSES;
  employees: EmployeeListItemDto[] = [];

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingRecord: AttendanceRecordDto | null = null;
  deleting = false;
  deleteError = '';

  constructor(
    private service: AttendanceService,
    private employeeService: EmployeeService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {
    const today = new Date().toISOString().slice(0, 10);
    this.filterFromDate = today;
    this.filterToDate = today;
  }

  ngOnInit(): void {
    this.buildForm();
    this.loadEmployees();
    this.load();
  }

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private buildForm(): void {
    this.form = this.fb.group({
      employeeId: [null as number | null, Validators.required],
      attendanceDate: [this.todayIso(), Validators.required],
      status: ['Present', Validators.required],
      checkInTime: [''],
      checkOutTime: [''],
      overtimeHours: [0, [Validators.required, Validators.min(0)]],
      notes: ['', Validators.maxLength(1000)]
    });
  }

  private loadEmployees(): void {
    this.employeeService.getAll({ page: 1, pageSize: 1000, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.employees = res.data.items;
        this.cdr.detectChanges();
      })
    });
  }

  employeeName(id: number | null | undefined): string {
    const e = id ? this.employees.find(x => x.id === id) : undefined;
    return e ? `${e.code} — ${e.fullName}` : '';
  }

  // ─── Data loading ──────────────────────────────────────────────────────────

  load(): void {
    this.loading = true;
    this.service.getAll(
      this.parameters,
      this.filterFromDate || undefined,
      this.filterToDate || undefined,
      this.filterEmployeeId ?? undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.records = res.data.items;
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

  // ─── Create / Edit ─────────────────────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.reset({
      employeeId: this.filterEmployeeId ?? null,
      attendanceDate: this.filterToDate || this.todayIso(),
      status: 'Present',
      checkInTime: '',
      checkOutTime: '',
      overtimeHours: 0,
      notes: ''
    });
    this.form.get('employeeId')?.enable();
    this.form.get('attendanceDate')?.enable();
    this.dialogVisible = true;
  }

  openEdit(rec: AttendanceRecordDto): void {
    this.dialogMode = 'edit';
    this.editingId = rec.id;
    this.dialogError = '';
    this.form.reset({
      employeeId: rec.employeeId,
      attendanceDate: rec.attendanceDate,
      status: rec.status,
      checkInTime: rec.checkInTime ?? '',
      checkOutTime: rec.checkOutTime ?? '',
      overtimeHours: rec.overtimeHours,
      notes: rec.notes ?? ''
    });
    // Employee + date are fixed once recorded.
    this.form.get('employeeId')?.disable();
    this.form.get('attendanceDate')?.disable();
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();
    const common = {
      status: v.status,
      checkInTime: (v.checkInTime as string)?.trim() || null,
      checkOutTime: (v.checkOutTime as string)?.trim() || null,
      overtimeHours: Number(v.overtimeHours) || 0,
      notes: (v.notes as string)?.trim() || null
    };

    if (this.dialogMode === 'create') {
      this.service.create({ ...common, employeeId: v.employeeId, attendanceDate: v.attendanceDate }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.service.update(this.editingId, common).subscribe({
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

  confirmDelete(rec: AttendanceRecordDto): void {
    this.deletingRecord = rec;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingRecord || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.service.delete(this.deletingRecord.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) {
          this.deleteDialogVisible = false;
          this.deletingRecord = null;
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
