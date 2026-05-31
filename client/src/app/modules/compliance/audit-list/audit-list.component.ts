import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ComplianceService } from '../../../services/compliance.service';
import { EmployeeService } from '../../../services/employee.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  ComplianceAuditListItemDto, ComplianceAuditDto, AuditFindingDto,
  AuditType, AuditStatus, FindingSeverity, FindingStatus,
  AUDIT_TYPES, AUDIT_STATUSES, AUDIT_RESULTS, SEVERITIES, FINDING_STATUSES
} from '../../../models/compliance.models';

@Component({
  selector: 'app-audit-list',
  standalone: false,
  templateUrl: './audit-list.component.html',
  styleUrl: './audit-list.component.scss'
})
export class AuditListComponent implements OnInit {
  audits: ComplianceAuditListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterType: AuditType | null = null;
  filterStatus: AuditStatus | null = null;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  employees: any[] = [];

  readonly types = AUDIT_TYPES;
  readonly statuses = AUDIT_STATUSES;
  readonly results = AUDIT_RESULTS;
  readonly severities = SEVERITIES;
  readonly findingStatuses = FINDING_STATUSES;

  canSchedule = false;
  canRecord = false;
  canManageCap = false;

  // Create / edit audit dialog
  auditDialogVisible = false;
  auditDialogMode: 'create' | 'edit' = 'create';
  auditSaving = false;
  auditError = '';
  editingAuditId: number | null = null;
  auditForm!: FormGroup;

  // Detail (audit + findings nested)
  detailVisible = false;
  detailLoading = false;
  detail: ComplianceAuditDto | null = null;

  // Finding dialog (add or edit)
  findingDialogVisible = false;
  findingMode: 'add' | 'edit' = 'add';
  findingSaving = false;
  findingError = '';
  editingFindingId: number | null = null;
  findingForm!: FormGroup;

  rowActionId: number | null = null;

  constructor(private svc: ComplianceService, private empService: EmployeeService,
              private auth: AuthService, private fb: FormBuilder,
              private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.canSchedule = this.auth.hasPermission('Compliance.ScheduleAudit');
    this.canRecord = this.auth.hasPermission('Compliance.RecordAudit');
    this.canManageCap = this.auth.hasPermission('Compliance.ManageCap');

    this.auditForm = this.fb.group({
      auditType: ['BSCI' as AuditType, Validators.required],
      auditor: ['', [Validators.required, Validators.maxLength(200)]],
      scheduledDate: [this.todayIso(), Validators.required],
      actualDate: [null as string | null],
      status: ['Scheduled' as AuditStatus, Validators.required],
      result: [null as string | null],
      score: [null as number | null, [Validators.min(0), Validators.max(100)]],
      notes: ['', Validators.maxLength(2000)]
    });

    this.findingForm = this.fb.group({
      findingDescription: ['', [Validators.required, Validators.maxLength(2000)]],
      severity: ['Minor' as FindingSeverity, Validators.required],
      correctiveAction: ['', Validators.maxLength(2000)],
      assignedToEmployeeId: [null as number | null],
      dueDate: [null as string | null],
      status: ['Open' as FindingStatus, Validators.required],
      closureDate: [null as string | null],
      notes: ['', Validators.maxLength(1000)]
    });

    this.loadEmployees();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  private loadEmployees(): void {
    this.empService.getAll({ page: 1, pageSize: 1000, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.employees = res.data.items; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getAudits(this.parameters, this.filterType, this.filterStatus).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.audits = res.data.items; this.totalCount = res.data.totalCount; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearch(v: string): void { if (this.searchTimer) clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => { this.parameters.search = v; this.parameters.page = 1; this.load(); }, 350); }
  onPage(e: any): void { this.parameters.page = Math.floor(e.first / e.rows) + 1; this.parameters.pageSize = e.rows; this.load(); }

  // ── Audit create / edit ──
  openCreateAudit(): void {
    this.auditDialogMode = 'create'; this.editingAuditId = null; this.auditError = '';
    this.auditForm.reset({
      auditType: 'BSCI', auditor: '', scheduledDate: this.todayIso(), actualDate: null,
      status: 'Scheduled', result: null, score: null, notes: ''
    });
    this.auditDialogVisible = true;
  }
  openEditAudit(a: ComplianceAuditListItemDto): void {
    this.auditDialogMode = 'edit'; this.editingAuditId = a.id; this.auditError = '';
    this.auditForm.reset({
      auditType: a.auditType, auditor: a.auditor,
      scheduledDate: a.scheduledDate, actualDate: a.actualDate,
      status: a.status, result: a.result, score: a.score, notes: ''
    });
    this.auditDialogVisible = true;
  }
  saveAudit(): void {
    if (this.auditForm.invalid || this.auditSaving) return;
    this.auditSaving = true; this.auditError = ''; this.cdr.detectChanges();
    const v = this.auditForm.getRawValue();
    const done = (res: any) => this.zone.run(() => { this.auditSaving = false; if (res.success) { this.auditDialogVisible = false; this.load(); } else this.auditError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.auditSaving = false; this.auditError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.auditDialogMode === 'create') {
      this.svc.createAudit({ auditType: v.auditType, auditor: v.auditor.trim(), scheduledDate: v.scheduledDate, notes: (v.notes as string)?.trim() || null })
        .subscribe({ next: done, error: err });
    } else {
      this.svc.updateAudit(this.editingAuditId!, {
        auditor: v.auditor.trim(), scheduledDate: v.scheduledDate, actualDate: v.actualDate,
        status: v.status, result: v.result, score: v.score,
        notes: (v.notes as string)?.trim() || null
      }).subscribe({ next: done, error: err });
    }
  }

  // ── Detail + Findings ──
  openDetail(a: ComplianceAuditListItemDto): void {
    this.detail = null; this.detailLoading = true; this.detailVisible = true; this.cdr.detectChanges();
    this.svc.getAuditById(a.id).subscribe({
      next: (res) => this.zone.run(() => { this.detailLoading = false; if (res.success && res.data) this.detail = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.detailLoading = false; this.cdr.detectChanges(); })
    });
  }
  refreshDetail(): void {
    if (!this.detail) return;
    this.svc.getAuditById(this.detail.id).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.detail = res.data; this.cdr.detectChanges(); })
    });
  }

  openAddFinding(): void {
    if (!this.detail) return;
    this.findingMode = 'add'; this.editingFindingId = null; this.findingError = '';
    this.findingForm.reset({
      findingDescription: '', severity: 'Minor', correctiveAction: '',
      assignedToEmployeeId: null, dueDate: null, status: 'Open', closureDate: null, notes: ''
    });
    this.findingDialogVisible = true;
  }
  openEditFinding(f: AuditFindingDto): void {
    this.findingMode = 'edit'; this.editingFindingId = f.id; this.findingError = '';
    this.findingForm.reset({
      findingDescription: f.findingDescription, severity: f.severity,
      correctiveAction: f.correctiveAction ?? '',
      assignedToEmployeeId: f.assignedToEmployeeId, dueDate: f.dueDate,
      status: f.status, closureDate: f.closureDate, notes: f.notes ?? ''
    });
    this.findingDialogVisible = true;
  }
  saveFinding(): void {
    if (!this.detail || this.findingForm.invalid || this.findingSaving) return;
    this.findingSaving = true; this.findingError = ''; this.cdr.detectChanges();
    const v = this.findingForm.getRawValue();
    const done = (res: any) => this.zone.run(() => { this.findingSaving = false; if (res.success) { this.findingDialogVisible = false; this.refreshDetail(); this.load(); } else this.findingError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.findingSaving = false; this.findingError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.findingMode === 'add') {
      this.svc.addFinding(this.detail.id, {
        findingDescription: v.findingDescription.trim(), severity: v.severity,
        correctiveAction: (v.correctiveAction as string)?.trim() || null,
        assignedToEmployeeId: v.assignedToEmployeeId, dueDate: v.dueDate,
        notes: (v.notes as string)?.trim() || null
      }).subscribe({ next: done, error: err });
    } else {
      this.svc.updateFinding(this.editingFindingId!, {
        findingDescription: v.findingDescription.trim(), severity: v.severity,
        correctiveAction: (v.correctiveAction as string)?.trim() || null,
        assignedToEmployeeId: v.assignedToEmployeeId, dueDate: v.dueDate,
        status: v.status, closureDate: v.closureDate,
        notes: (v.notes as string)?.trim() || null
      }).subscribe({ next: done, error: err });
    }
  }
  deleteFinding(f: AuditFindingDto): void {
    if (this.rowActionId) return;
    this.rowActionId = f.id; this.cdr.detectChanges();
    this.svc.deleteFinding(f.id).subscribe({
      next: () => this.zone.run(() => { this.rowActionId = null; this.refreshDetail(); this.load(); }),
      error: () => this.zone.run(() => { this.rowActionId = null; this.cdr.detectChanges(); })
    });
  }

  auditStatusSeverity(s: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    return s === 'Scheduled' ? 'info' : s === 'InProgress' ? 'warn' : s === 'Completed' ? 'success' : 'secondary';
  }
  resultSeverity(r: string | null): 'success' | 'warn' | 'danger' | 'secondary' {
    if (!r) return 'secondary';
    return r === 'Pass' ? 'success' : r === 'Conditional' || r === 'PendingCorrection' ? 'warn' : 'danger';
  }
  severityColor(sev: string): string {
    switch (sev) {
      case 'Critical': return '#dc2626';
      case 'Major': return '#d97706';
      case 'Minor': return '#2563eb';
      case 'Observation': return '#6b7280';
      default: return '#6b7280';
    }
  }
  findingStatusSeverity(s: string): 'success' | 'info' | 'warn' | 'secondary' {
    return s === 'Open' ? 'warn' : s === 'InProgress' ? 'info' : s === 'Closed' ? 'success' : 'secondary';
  }
}
