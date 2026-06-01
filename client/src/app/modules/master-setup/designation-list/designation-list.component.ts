import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MasterSetupService } from '../../../services/master-setup.service';
import { AuthService } from '../../../services/auth.service';
import { DesignationDto } from '../../../models/master-setup.models';

@Component({
  selector: 'app-designation-list',
  standalone: false,
  templateUrl: './designation-list.component.html',
  styleUrl: './designation-list.component.scss'
})
export class DesignationListComponent implements OnInit {
  items: DesignationDto[] = [];
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
  deleting: DesignationDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(private svc: MasterSetupService, private auth: AuthService,
              private fb: FormBuilder, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('MasterSetup.ManageDesignations');
    this.form = this.fb.group({
      code: ['', Validators.maxLength(30)],
      name: ['', [Validators.required, Validators.maxLength(150)]],
      gradeLevel: [null as number | null, [Validators.min(1), Validators.max(10)]],
      description: ['', Validators.maxLength(500)],
      isActive: [true]
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getDesignations(this.includeInactive).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.items = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({ code: '', name: '', gradeLevel: null, description: '', isActive: true });
    this.dialogVisible = true;
  }
  openEdit(d: DesignationDto): void {
    this.dialogMode = 'edit'; this.editingId = d.id; this.dialogError = '';
    this.form.reset({ code: d.code ?? '', name: d.name, gradeLevel: d.gradeLevel, description: d.description ?? '', isActive: d.isActive });
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = {
      code: (v.code as string)?.trim() || null, name: v.name.trim(),
      gradeLevel: v.gradeLevel, description: (v.description as string)?.trim() || null
    };
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.createDesignation(base).subscribe({ next: done, error: err });
    else this.svc.updateDesignation(this.editingId!, { ...base, isActive: v.isActive }).subscribe({ next: done, error: err });
  }

  confirmDelete(d: DesignationDto): void { this.deleting = d; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.deleteDesignation(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }
}
