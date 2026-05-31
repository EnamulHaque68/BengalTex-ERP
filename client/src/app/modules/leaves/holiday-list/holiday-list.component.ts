import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LeavesService } from '../../../services/leaves.service';
import { AuthService } from '../../../services/auth.service';
import { HolidayDto } from '../../../models/leaves.models';

@Component({
  selector: 'app-holiday-list',
  standalone: false,
  templateUrl: './holiday-list.component.html',
  styleUrl: './holiday-list.component.scss'
})
export class HolidayListComponent implements OnInit {
  holidays: HolidayDto[] = [];
  loading = false;
  filterYear: number = new Date().getFullYear();
  includeInactive = false;

  canManage = false;
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deleting: HolidayDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(private svc: LeavesService, private auth: AuthService, private fb: FormBuilder,
              private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('Leaves.ManageHolidays');
    this.form = this.fb.group({
      date: [this.todayIso(), Validators.required],
      name: ['', [Validators.required, Validators.maxLength(150)]],
      description: ['', Validators.maxLength(500)],
      isActive: [true]
    });
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  load(): void {
    this.loading = true;
    this.svc.getHolidays(this.filterYear, this.includeInactive).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.holidays = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({ date: this.todayIso(), name: '', description: '', isActive: true });
    this.dialogVisible = true;
  }
  openEdit(h: HolidayDto): void {
    this.dialogMode = 'edit'; this.editingId = h.id; this.dialogError = '';
    this.form.reset({ date: h.date, name: h.name, description: h.description ?? '', isActive: h.isActive });
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = { date: v.date, name: v.name.trim(), description: (v.description as string)?.trim() || null };
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.createHoliday(base).subscribe({ next: done, error: err });
    else this.svc.updateHoliday(this.editingId!, { ...base, isActive: v.isActive }).subscribe({ next: done, error: err });
  }

  confirmDelete(h: HolidayDto): void { this.deleting = h; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.deleteHoliday(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }

  dayOfWeek(iso: string): string {
    if (!iso) return '';
    return new Date(iso).toLocaleDateString('en-US', { weekday: 'short' });
  }
}
