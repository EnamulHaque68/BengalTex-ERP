import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MasterSetupService } from '../../../services/master-setup.service';
import { AuthService } from '../../../services/auth.service';
import { ShiftDto, DAYS_OF_WEEK, DayOfWeek } from '../../../models/master-setup.models';

@Component({
  selector: 'app-shift-list',
  standalone: false,
  templateUrl: './shift-list.component.html',
  styleUrl: './shift-list.component.scss'
})
export class ShiftListComponent implements OnInit {
  items: ShiftDto[] = [];
  loading = false;
  includeInactive = false;
  canManage = false;

  readonly days = DAYS_OF_WEEK;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deleting: ShiftDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(private svc: MasterSetupService, private auth: AuthService,
              private fb: FormBuilder, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('MasterSetup.ManageShifts');
    this.form = this.fb.group({
      code: ['', [Validators.required, Validators.maxLength(20)]],
      name: ['', [Validators.required, Validators.maxLength(100)]],
      startTime: ['08:00', Validators.required],
      endTime: ['17:00', Validators.required],
      weekendDayOfWeek: ['Friday' as DayOfWeek, Validators.required],
      secondWeekendDayOfWeek: [null as DayOfWeek | null],
      description: ['', Validators.maxLength(500)],
      isActive: [true]
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getShifts(this.includeInactive).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.items = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({ code: '', name: '', startTime: '08:00', endTime: '17:00',
                      weekendDayOfWeek: 'Friday', secondWeekendDayOfWeek: null, description: '', isActive: true });
    this.form.get('code')?.enable();
    this.dialogVisible = true;
  }
  openEdit(s: ShiftDto): void {
    this.dialogMode = 'edit'; this.editingId = s.id; this.dialogError = '';
    this.form.reset({ code: s.code, name: s.name, startTime: s.startTime, endTime: s.endTime,
                      weekendDayOfWeek: s.weekendDayOfWeek, secondWeekendDayOfWeek: s.secondWeekendDayOfWeek,
                      description: s.description ?? '', isActive: s.isActive });
    this.form.get('code')?.disable();
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') {
      this.svc.createShift({
        code: (v.code as string).trim().toUpperCase(), name: v.name.trim(),
        startTime: v.startTime, endTime: v.endTime,
        weekendDayOfWeek: v.weekendDayOfWeek, secondWeekendDayOfWeek: v.secondWeekendDayOfWeek,
        description: (v.description as string)?.trim() || null
      }).subscribe({ next: done, error: err });
    } else {
      this.svc.updateShift(this.editingId!, {
        name: v.name.trim(), startTime: v.startTime, endTime: v.endTime,
        weekendDayOfWeek: v.weekendDayOfWeek, secondWeekendDayOfWeek: v.secondWeekendDayOfWeek,
        description: (v.description as string)?.trim() || null, isActive: v.isActive
      }).subscribe({ next: done, error: err });
    }
  }

  confirmDelete(s: ShiftDto): void { this.deleting = s; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.deleteShift(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }
}
