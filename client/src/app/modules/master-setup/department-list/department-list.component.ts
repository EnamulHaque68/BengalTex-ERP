import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MasterSetupService } from '../../../services/master-setup.service';
import { EmployeeService } from '../../../services/employee.service';
import { AuthService } from '../../../services/auth.service';
import { DepartmentDto } from '../../../models/master-setup.models';

@Component({
  selector: 'app-department-list',
  standalone: false,
  templateUrl: './department-list.component.html',
  styleUrl: './department-list.component.scss'
})
export class DepartmentListComponent implements OnInit {
  items: DepartmentDto[] = [];
  loading = false;
  includeInactive = false;
  employees: any[] = [];

  canManage = false;
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deleting: DepartmentDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(private svc: MasterSetupService, private empService: EmployeeService,
              private auth: AuthService, private fb: FormBuilder,
              private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('MasterSetup.ManageDepartments');
    this.form = this.fb.group({
      code: ['', Validators.maxLength(30)],
      name: ['', [Validators.required, Validators.maxLength(150)]],
      parentDepartmentId: [null as number | null],
      headEmployeeId: [null as number | null],
      description: ['', Validators.maxLength(500)],
      isActive: [true]
    });
    this.empService.getAll({ page: 1, pageSize: 1000, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.employees = res.data.items; this.cdr.detectChanges(); })
    });
    this.load();
  }

  get parentOptions(): DepartmentDto[] {
    return this.items.filter(d => d.id !== this.editingId);
  }

  load(): void {
    this.loading = true;
    this.svc.getDepartments(this.includeInactive).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.items = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({ code: '', name: '', parentDepartmentId: null, headEmployeeId: null, description: '', isActive: true });
    this.dialogVisible = true;
  }
  openEdit(d: DepartmentDto): void {
    this.dialogMode = 'edit'; this.editingId = d.id; this.dialogError = '';
    this.form.reset({ code: d.code ?? '', name: d.name, parentDepartmentId: d.parentDepartmentId,
                      headEmployeeId: d.headEmployeeId, description: d.description ?? '', isActive: d.isActive });
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = {
      code: (v.code as string)?.trim() || null, name: v.name.trim(),
      parentDepartmentId: v.parentDepartmentId, headEmployeeId: v.headEmployeeId,
      description: (v.description as string)?.trim() || null
    };
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.createDepartment(base).subscribe({ next: done, error: err });
    else this.svc.updateDepartment(this.editingId!, { ...base, isActive: v.isActive }).subscribe({ next: done, error: err });
  }

  confirmDelete(d: DepartmentDto): void { this.deleting = d; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.deleteDepartment(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }
}
