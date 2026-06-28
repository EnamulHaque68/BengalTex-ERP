import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { WorkCenterService } from '../../../services/work-center.service';
import { AuthService } from '../../../services/auth.service';
import { WorkCenterDto } from '../../../models/work-center.models';

@Component({
  selector: 'app-work-center-list',
  standalone: false,
  templateUrl: './work-center-list.component.html',
  styleUrl: './work-center-list.component.scss'
})
export class WorkCenterListComponent implements OnInit {
  workCenters: WorkCenterDto[] = [];
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
  deleting: WorkCenterDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(
    private svc: WorkCenterService,
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
      code: ['', [Validators.required, Validators.maxLength(50)]],
      name: ['', [Validators.required, Validators.maxLength(200)]],
      type: ['', Validators.maxLength(80)],
      location: ['', Validators.maxLength(150)],
      capacityPerDay: [null as number | null, [Validators.min(0)]],
      costPerHour: [null as number | null, [Validators.min(0)]],
      notes: ['', Validators.maxLength(1000)],
      isActive: [true]
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.includeInactive).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.workCenters = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  loadClass(w: WorkCenterDto): string {
    if (w.loadPercent == null) return '';
    if (w.loadPercent > 100) return 'load-over';
    if (w.loadPercent >= 80) return 'load-high';
    return 'load-ok';
  }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({ code: '', name: '', type: '', location: '', capacityPerDay: null, costPerHour: null, notes: '', isActive: true });
    this.form.get('code')?.enable();
    this.dialogVisible = true;
  }

  openEdit(w: WorkCenterDto): void {
    this.dialogMode = 'edit'; this.editingId = w.id; this.dialogError = '';
    this.form.reset({
      code: w.code, name: w.name, type: w.type ?? '', location: w.location ?? '',
      capacityPerDay: w.capacityPerDay, costPerHour: w.costPerHour, notes: w.notes ?? '', isActive: w.isActive
    });
    this.form.get('code')?.disable();   // code is immutable after creation
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = {
      name: v.name,
      type: (v.type as string)?.trim() || null,
      location: (v.location as string)?.trim() || null,
      capacityPerDay: v.capacityPerDay,
      costPerHour: v.costPerHour,
      notes: (v.notes as string)?.trim() || null
    };
    const done = (res: any) => this.zone.run(() => {
      this.dialogSaving = false;
      if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.';
      this.cdr.detectChanges();
    });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.create({ ...base, code: (v.code as string)?.trim() }).subscribe({ next: done, error: err });
    else this.svc.update(this.editingId!, { ...base, isActive: v.isActive }).subscribe({ next: done, error: err });
  }

  confirmDelete(w: WorkCenterDto): void { this.deleting = w; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.delete(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleteBusy = false;
        if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.';
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }
}
