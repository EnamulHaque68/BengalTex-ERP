import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LeavesService } from '../../../services/leaves.service';
import { AuthService } from '../../../services/auth.service';
import { LeaveTypeDto } from '../../../models/leaves.models';

@Component({
  selector: 'app-leave-type-list',
  standalone: false,
  templateUrl: './leave-type-list.component.html',
  styleUrl: './leave-type-list.component.scss'
})
export class LeaveTypeListComponent implements OnInit {
  types: LeaveTypeDto[] = [];
  loading = false;
  includeInactive = false;

  canManage = false;
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deleting: LeaveTypeDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(private svc: LeavesService, private auth: AuthService, private fb: FormBuilder,
              private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('Leaves.ManageTypes');
    this.form = this.fb.group({
      code: ['', [Validators.required, Validators.maxLength(20)]],
      name: ['', [Validators.required, Validators.maxLength(100)]],
      isPaid: [true],
      annualEntitlement: [0, [Validators.required, Validators.min(0)]],
      maxConsecutiveDays: [null as number | null, [Validators.min(1)]],
      description: ['', Validators.maxLength(500)],
      isActive: [true]
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getLeaveTypes(this.includeInactive).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.types = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({ code: '', name: '', isPaid: true, annualEntitlement: 0, maxConsecutiveDays: null, description: '', isActive: true });
    this.form.get('code')?.enable();
    this.dialogVisible = true;
  }
  openEdit(t: LeaveTypeDto): void {
    this.dialogMode = 'edit'; this.editingId = t.id; this.dialogError = '';
    this.form.reset({ code: t.code, name: t.name, isPaid: t.isPaid, annualEntitlement: t.annualEntitlement, maxConsecutiveDays: t.maxConsecutiveDays, description: t.description ?? '', isActive: t.isActive });
    this.form.get('code')?.disable();
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = {
      name: v.name, isPaid: !!v.isPaid,
      annualEntitlement: Number(v.annualEntitlement) || 0,
      maxConsecutiveDays: v.maxConsecutiveDays ?? null,
      description: (v.description as string)?.trim() || null
    };
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.createLeaveType({ ...base, code: (v.code as string).trim().toUpperCase() }).subscribe({ next: done, error: err });
    else this.svc.updateLeaveType(this.editingId!, { ...base, isActive: v.isActive }).subscribe({ next: done, error: err });
  }

  confirmDelete(t: LeaveTypeDto): void { this.deleting = t; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.deleteLeaveType(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }
}
