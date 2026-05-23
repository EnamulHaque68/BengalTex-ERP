import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { WastageService } from '../../../services/wastage.service';
import { AuthService } from '../../../services/auth.service';
import { WastageReasonDto } from '../../../models/wastage.models';

@Component({
  selector: 'app-wastage-reason-list',
  standalone: false,
  templateUrl: './wastage-reason-list.component.html',
  styleUrl: './wastage-reason-list.component.scss'
})
export class WastageReasonListComponent implements OnInit {
  reasons: WastageReasonDto[] = [];
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
  deleting: WastageReasonDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(private svc: WastageService, private auth: AuthService, private fb: FormBuilder, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('Wastage.ManageReasons');
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      isReusable: [false],
      isActive: [true],
      description: ['', Validators.maxLength(500)]
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getReasons(this.includeInactive).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.reasons = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  openCreate(): void { this.dialogMode = 'create'; this.editingId = null; this.dialogError = ''; this.form.reset({ name: '', isReusable: false, isActive: true, description: '' }); this.dialogVisible = true; }
  openEdit(r: WastageReasonDto): void { this.dialogMode = 'edit'; this.editingId = r.id; this.dialogError = ''; this.form.reset({ name: r.name, isReusable: r.isReusable, isActive: r.isActive, description: r.description ?? '' }); this.dialogVisible = true; }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = { name: (v.name as string).trim(), isReusable: !!v.isReusable, description: (v.description as string)?.trim() || null };
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.createReason(base).subscribe({ next: done, error: err });
    else this.svc.updateReason(this.editingId!, { id: this.editingId!, isActive: !!v.isActive, ...base }).subscribe({ next: done, error: err });
  }

  confirmDelete(r: WastageReasonDto): void { this.deleting = r; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.deleteReason(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }
}
