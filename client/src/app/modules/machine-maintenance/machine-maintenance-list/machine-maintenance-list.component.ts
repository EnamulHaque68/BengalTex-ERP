import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MachineMaintenanceService } from '../../../services/machine-maintenance.service';
import { JobCardService } from '../../../services/job-card.service';
import { EmployeeService } from '../../../services/employee.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  MachineMaintenanceDto, MAINTENANCE_TYPES, MAINTENANCE_STATUSES
} from '../../../models/machine-maintenance.models';
import { MachineDto } from '../../../models/job-card.models';
import { EmployeeListItemDto } from '../../../models/employee.models';

@Component({
  selector: 'app-machine-maintenance-list',
  standalone: false,
  templateUrl: './machine-maintenance-list.component.html',
  styleUrl: './machine-maintenance-list.component.scss'
})
export class MachineMaintenanceListComponent implements OnInit {
  records: MachineMaintenanceDto[] = [];
  loading = false;
  totalCount = 0;
  actionError = '';
  actionMessage = '';
  rowActionId: number | null = null;

  readonly types = MAINTENANCE_TYPES;
  readonly statuses = MAINTENANCE_STATUSES;
  machines: MachineDto[] = [];
  employees: EmployeeListItemDto[] = [];

  filterStatus: string | null = null;
  filterType: string | null = null;
  filterMachineId: number | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Schedule / Edit dialog
  dialogVisible = false;
  dialogSaving = false;
  dialogError = '';
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  editing: MachineMaintenanceDto | null = null;
  form!: FormGroup;

  // Complete dialog
  completeVisible = false;
  completeTarget: MachineMaintenanceDto | null = null;
  completeForm!: FormGroup;
  completing = false;
  completeError = '';

  constructor(
    private svc: MachineMaintenanceService,
    private jobCardSvc: JobCardService,
    private employeeSvc: EmployeeService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      machineId: [null, Validators.required],
      type: ['Preventive', Validators.required],
      description: ['', [Validators.required, Validators.maxLength(500)]],
      scheduledDate: [this.todayIso(), Validators.required],
      isRecurring: [false],
      intervalDays: [null, [Validators.min(1), Validators.max(3650)]],
      notes: ['', Validators.maxLength(2000)]
    });
    this.completeForm = this.fb.group({
      completedDate: [this.todayIso(), Validators.required],
      downtimeHours: [null, Validators.min(0)],
      performedBy: ['', Validators.maxLength(150)],
      performedByEmployeeId: [null],
      serviceCost: [0, [Validators.required, Validators.min(0)]],
      partsCost: [0, [Validators.required, Validators.min(0)]],
      partsReplaced: ['', Validators.maxLength(1000)],
      completionNotes: ['', Validators.maxLength(2000)]
    });
    this.loadMasters();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().substring(0, 10); }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }

  typeLabel(t: string): string { return this.types.find(x => x.value === t)?.label ?? t; }

  statusBadgeClass(s: string): string {
    switch (s) {
      case 'Scheduled': return 'scheduled';
      case 'InProgress': return 'inprogress';
      case 'Completed': return 'completed';
      case 'Cancelled': return 'cancelled';
      default: return '';
    }
  }

  typeBadgeClass(t: string): string {
    switch (t) {
      case 'Preventive': return 'preventive';
      case 'Corrective': return 'corrective';
      case 'Inspection': return 'inspection';
      case 'Calibration': return 'calibration';
      case 'Overhaul': return 'overhaul';
      case 'Cleaning': return 'cleaning';
      default: return '';
    }
  }

  private loadMasters(): void {
    this.jobCardSvc.getMachines(false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.machines = res.data;
        this.cdr.detectChanges();
      })
    });
    this.employeeSvc.getAll({ page: 1, pageSize: 1000, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.employees = res.data.items;
        this.cdr.detectChanges();
      })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters,
      this.filterStatus ?? undefined, this.filterType ?? undefined,
      this.filterMachineId ?? undefined).subscribe({
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

  // ── Schedule / Edit ─────────────────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editing = null;
    this.dialogError = '';
    this.form.reset({
      machineId: null, type: 'Preventive', description: '',
      scheduledDate: this.todayIso(),
      isRecurring: false, intervalDays: null, notes: ''
    });
    this.form.enable();
    this.dialogVisible = true;
  }

  openEdit(m: MachineMaintenanceDto): void {
    this.dialogMode = m.status === 'Scheduled' ? 'edit' : 'view';
    this.editing = m;
    this.dialogError = '';
    this.form.reset({
      machineId: m.machineId,
      type: m.type,
      description: m.description,
      scheduledDate: m.scheduledDate,
      isRecurring: m.isRecurring,
      intervalDays: m.intervalDays,
      notes: m.notes ?? ''
    });
    if (this.dialogMode === 'view') this.form.disable(); else this.form.enable();
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const body: any = {
      machineId: Number(v.machineId),
      type: v.type,
      description: v.description.trim(),
      scheduledDate: v.scheduledDate,
      isRecurring: !!v.isRecurring,
      intervalDays: v.isRecurring ? (v.intervalDays ? Number(v.intervalDays) : null) : null,
      notes: (v.notes as string)?.trim() || null
    };
    const obs: any = this.dialogMode === 'create' ? this.svc.schedule(body) : this.svc.update(this.editing!.id, body);
    obs.subscribe({
      next: (res: any) => this.zone.run(() => {
        this.dialogSaving = false;
        if (res.success) { this.dialogVisible = false; this.actionMessage = res.message || 'Saved.'; this.load(); }
        else this.dialogError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err: any) => this.zone.run(() => {
        this.dialogSaving = false;
        this.dialogError = err?.error?.message || 'Save failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Start / Cancel / Delete ────────────────────────────────────────────

  doAction(m: MachineMaintenanceDto, action: 'start' | 'cancel' | 'delete'): void {
    const labels: Record<string, string> = {
      start: 'Mark this maintenance as In Progress?',
      cancel: 'Cancel this maintenance?',
      delete: 'Delete this scheduled maintenance? (soft delete)'
    };
    if (!confirm(labels[action])) return;
    if (this.rowActionId) return;
    this.rowActionId = m.id;
    this.actionError = '';
    this.cdr.detectChanges();
    const obs = action === 'start' ? this.svc.start(m.id)
              : action === 'cancel' ? this.svc.cancel(m.id)
              : this.svc.delete(m.id);
    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) { this.actionMessage = res.message || 'Done.'; this.load(); }
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

  // ── Complete ────────────────────────────────────────────────────────────

  openComplete(m: MachineMaintenanceDto): void {
    this.completeTarget = m;
    this.completeError = '';
    this.completeForm.reset({
      completedDate: this.todayIso(),
      downtimeHours: null,
      performedBy: '', performedByEmployeeId: null,
      serviceCost: 0, partsCost: 0,
      partsReplaced: '', completionNotes: ''
    });
    this.completeVisible = true;
  }

  doComplete(): void {
    if (!this.completeTarget || this.completeForm.invalid || this.completing) return;
    this.completing = true; this.completeError = ''; this.cdr.detectChanges();
    const v = this.completeForm.getRawValue();
    this.svc.complete(this.completeTarget.id, {
      completedDate: v.completedDate,
      downtimeHours: v.downtimeHours != null ? Number(v.downtimeHours) : null,
      performedBy: (v.performedBy as string)?.trim() || null,
      performedByEmployeeId: v.performedByEmployeeId ?? null,
      serviceCost: Number(v.serviceCost) || 0,
      partsCost: Number(v.partsCost) || 0,
      partsReplaced: (v.partsReplaced as string)?.trim() || null,
      completionNotes: (v.completionNotes as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.completing = false;
        if (res.success) { this.completeVisible = false; this.actionMessage = res.message || 'Completed.'; this.load(); }
        else this.completeError = res.message || 'Action failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.completing = false;
        this.completeError = err?.error?.message || 'Action failed.';
        this.cdr.detectChanges();
      })
    });
  }
}
