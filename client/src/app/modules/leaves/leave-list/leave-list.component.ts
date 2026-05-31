import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LeavesService } from '../../../services/leaves.service';
import { EmployeeService } from '../../../services/employee.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  LeaveApplicationListItemDto, LeaveApplicationStatus, LEAVE_STATUSES, LeaveTypeDto
} from '../../../models/leaves.models';

@Component({
  selector: 'app-leave-list',
  standalone: false,
  templateUrl: './leave-list.component.html',
  styleUrl: './leave-list.component.scss'
})
export class LeaveListComponent implements OnInit {

  leaves: LeaveApplicationListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: LeaveApplicationStatus | null = null;
  filterEmployeeId: number | null = null;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  leaveTypes: LeaveTypeDto[] = [];
  employees: any[] = [];

  readonly statuses = LEAVE_STATUSES;

  canApply = false;
  canApprove = false;
  canCancel = false;

  // Apply dialog
  applyVisible = false;
  applySaving = false;
  applyError = '';
  applyForm!: FormGroup;

  // Reject dialog
  rejectVisible = false;
  rejectBusy = false;
  rejectError = '';
  rejectTarget: LeaveApplicationListItemDto | null = null;
  rejectReason = '';

  rowActionId: number | null = null;

  constructor(
    private svc: LeavesService,
    private empService: EmployeeService,
    private auth: AuthService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canApply = this.auth.hasPermission('Leaves.Apply');
    this.canApprove = this.auth.hasPermission('Leaves.Approve');
    this.canCancel = this.auth.hasPermission('Leaves.Cancel');

    this.applyForm = this.fb.group({
      employeeId: [null as number | null, Validators.required],
      leaveTypeId: [null as number | null, Validators.required],
      fromDate: [this.todayIso(), Validators.required],
      toDate: [this.todayIso(), Validators.required],
      reason: ['', Validators.maxLength(1000)],
      writeAttendance: [true],
      notes: ['', Validators.maxLength(1000)]
    });

    this.loadDropdowns();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  private loadDropdowns(): void {
    this.svc.getLeaveTypes(false).subscribe({ next: (res) => this.zone.run(() => { if (res.success && res.data) this.leaveTypes = res.data; this.cdr.detectChanges(); }) });
    this.empService.getAll({ page: 1, pageSize: 1000, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.employees = res.data.items; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters, this.filterStatus, this.filterEmployeeId ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.leaves = res.data.items; this.totalCount = res.data.totalCount; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearch(v: string): void { if (this.searchTimer) clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => { this.parameters.search = v; this.parameters.page = 1; this.load(); }, 350); }
  onPage(e: any): void { this.parameters.page = Math.floor(e.first / e.rows) + 1; this.parameters.pageSize = e.rows; this.load(); }

  openApply(): void {
    this.applyError = '';
    this.applyForm.reset({ employeeId: null, leaveTypeId: null, fromDate: this.todayIso(), toDate: this.todayIso(), reason: '', writeAttendance: true, notes: '' });
    this.applyVisible = true;
  }

  doApply(): void {
    if (this.applyForm.invalid || this.applySaving) return;
    this.applySaving = true; this.applyError = ''; this.cdr.detectChanges();
    const v = this.applyForm.getRawValue();
    this.svc.create({
      employeeId: v.employeeId, leaveTypeId: v.leaveTypeId,
      fromDate: v.fromDate, toDate: v.toDate,
      reason: (v.reason as string)?.trim() || null,
      writeAttendance: !!v.writeAttendance,
      notes: (v.notes as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => { this.applySaving = false; if (res.success) { this.applyVisible = false; this.load(); } else this.applyError = res.message || 'Submit failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.applySaving = false; this.applyError = e?.error?.message || 'Submit failed.'; this.cdr.detectChanges(); })
    });
  }

  approve(l: LeaveApplicationListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = l.id; this.cdr.detectChanges();
    this.svc.approve(l.id).subscribe({
      next: () => this.zone.run(() => { this.rowActionId = null; this.load(); }),
      error: () => this.zone.run(() => { this.rowActionId = null; this.cdr.detectChanges(); })
    });
  }

  openReject(l: LeaveApplicationListItemDto): void { this.rejectTarget = l; this.rejectReason = ''; this.rejectError = ''; this.rejectVisible = true; }
  doReject(): void {
    if (!this.rejectTarget || this.rejectBusy) return;
    this.rejectBusy = true; this.rejectError = ''; this.cdr.detectChanges();
    this.svc.reject(this.rejectTarget.id, this.rejectReason.trim() || null).subscribe({
      next: (res) => this.zone.run(() => { this.rejectBusy = false; if (res.success) { this.rejectVisible = false; this.rejectTarget = null; this.load(); } else this.rejectError = res.message || 'Reject failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.rejectBusy = false; this.rejectError = e?.error?.message || 'Reject failed.'; this.cdr.detectChanges(); })
    });
  }

  cancel(l: LeaveApplicationListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = l.id; this.cdr.detectChanges();
    this.svc.cancel(l.id).subscribe({
      next: () => this.zone.run(() => { this.rowActionId = null; this.load(); }),
      error: () => this.zone.run(() => { this.rowActionId = null; this.cdr.detectChanges(); })
    });
  }

  statusSeverity(s: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (s) {
      case 'Pending': return 'warn';
      case 'Approved': return 'success';
      case 'Rejected': return 'danger';
      case 'Cancelled': return 'secondary';
      default: return 'secondary';
    }
  }
}
