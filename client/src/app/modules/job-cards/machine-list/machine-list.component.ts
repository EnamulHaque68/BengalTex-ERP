import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { JobCardService } from '../../../services/job-card.service';
import { AuthService } from '../../../services/auth.service';
import { MachineDto } from '../../../models/job-card.models';

@Component({
  selector: 'app-machine-list',
  standalone: false,
  templateUrl: './machine-list.component.html',
  styleUrl: './machine-list.component.scss'
})
export class MachineListComponent implements OnInit {
  machines: MachineDto[] = [];
  loading = false;
  includeInactive = false;

  canCreate = false;
  canEdit = false;
  canDelete = false;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deleting: MachineDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(
    private svc: JobCardService,
    private auth: AuthService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canCreate = this.auth.hasPermission('Machines.Create');
    this.canEdit = this.auth.hasPermission('Machines.Edit');
    this.canDelete = this.auth.hasPermission('Machines.Delete');
    this.form = this.fb.group({
      code: [''],
      name: ['', [Validators.required, Validators.maxLength(200)]],
      machineType: ['', Validators.maxLength(80)],
      location: ['', Validators.maxLength(150)],
      capacityPerHour: [null as number | null, [Validators.min(0)]],
      notes: ['', Validators.maxLength(1000)],
      isActive: [true]
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getMachines(this.includeInactive).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.machines = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({ code: '', name: '', machineType: '', location: '', capacityPerHour: null, notes: '', isActive: true });
    this.dialogVisible = true;
  }
  openEdit(m: MachineDto): void {
    this.dialogMode = 'edit'; this.editingId = m.id; this.dialogError = '';
    this.form.reset({
      code: m.code, name: m.name, machineType: m.machineType ?? '', location: m.location ?? '',
      capacityPerHour: m.capacityPerHour, notes: m.notes ?? '', isActive: m.isActive
    });
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = {
      name: v.name, machineType: (v.machineType as string)?.trim() || null,
      location: (v.location as string)?.trim() || null,
      capacityPerHour: v.capacityPerHour, notes: (v.notes as string)?.trim() || null
    };
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.createMachine({ ...base, code: (v.code as string)?.trim() || null }).subscribe({ next: done, error: err });
    else this.svc.updateMachine(this.editingId!, { ...base, isActive: v.isActive }).subscribe({ next: done, error: err });
  }

  confirmDelete(m: MachineDto): void { this.deleting = m; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.deleteMachine(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }
}
