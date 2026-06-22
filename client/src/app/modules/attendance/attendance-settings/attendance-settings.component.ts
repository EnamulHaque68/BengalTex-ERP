import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AttendanceService } from '../../../services/attendance.service';
import { AttendanceSettingsDto } from '../../../models/attendance.models';

@Component({
  selector: 'app-attendance-settings',
  standalone: false,
  templateUrl: './attendance-settings.component.html',
  styleUrl: './attendance-settings.component.scss'
})
export class AttendanceSettingsComponent implements OnInit {
  loading = true;
  saving = false;
  error = '';
  saved = false;
  model: AttendanceSettingsDto | null = null;

  constructor(private svc: AttendanceService, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.svc.getSettings().subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.model = res.data;
        else this.error = res.message || 'Could not load settings.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loading = false; this.error = err?.error?.message || 'Could not load settings.';
        this.cdr.detectChanges();
      })
    });
  }

  save(): void {
    if (!this.model || this.saving) return;
    this.saving = true; this.error = ''; this.saved = false;
    const { id, ...body } = this.model;
    this.svc.updateSettings(body).subscribe({
      next: (res) => this.zone.run(() => {
        this.saving = false;
        if (res.success && res.data) { this.model = res.data; this.saved = true; setTimeout(() => { this.saved = false; this.cdr.detectChanges(); }, 2500); }
        else this.error = res.message || 'Could not save.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.saving = false; this.error = err?.error?.message || 'Could not save.';
        this.cdr.detectChanges();
      })
    });
  }
}
