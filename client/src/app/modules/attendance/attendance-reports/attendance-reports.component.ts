import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AttendanceService } from '../../../services/attendance.service';
import {
  DailyRegisterDto, MonthlySummaryDto, AttendanceExceptionsDto, ATTENDANCE_EXCEPTION_TYPES
} from '../../../models/attendance.models';

@Component({
  selector: 'app-attendance-reports',
  standalone: false,
  templateUrl: './attendance-reports.component.html',
  styleUrl: './attendance-reports.component.scss'
})
export class AttendanceReportsComponent implements OnInit {
  tab: 'daily' | 'monthly' | 'exceptions' = 'daily';
  exceptionTypes = ATTENDANCE_EXCEPTION_TYPES;

  // daily
  date = '';
  dailyLoading = false; dailyError = ''; daily: DailyRegisterDto | null = null;

  // monthly
  year = new Date().getFullYear();
  month = new Date().getMonth() + 1;
  months = Array.from({ length: 12 }, (_, i) => ({ v: i + 1, l: new Date(2000, i, 1).toLocaleString('en', { month: 'long' }) }));
  monthlyLoading = false; monthlyError = ''; monthly: MonthlySummaryDto | null = null;

  // exceptions
  exFrom = ''; exTo = ''; exType = 'Late';
  exLoading = false; exError = ''; exceptions: AttendanceExceptionsDto | null = null;

  constructor(private svc: AttendanceService, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    const today = new Date().toISOString().slice(0, 10);
    this.date = today; this.exFrom = today; this.exTo = today;
    this.loadDaily();
  }

  switchTab(t: 'daily' | 'monthly' | 'exceptions'): void {
    this.tab = t;
    if (t === 'monthly' && !this.monthly) this.loadMonthly();
    if (t === 'exceptions' && !this.exceptions) this.loadExceptions();
  }

  loadDaily(): void {
    this.dailyLoading = true; this.dailyError = '';
    this.svc.getDailyRegister(this.date).subscribe({
      next: (res) => this.zone.run(() => {
        this.dailyLoading = false;
        if (res.success && res.data) this.daily = res.data; else this.dailyError = res.message || 'Failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.dailyLoading = false; this.dailyError = err?.error?.message || 'Failed.'; this.cdr.detectChanges(); })
    });
  }

  loadMonthly(): void {
    this.monthlyLoading = true; this.monthlyError = '';
    this.svc.getMonthlySummary(this.year, this.month).subscribe({
      next: (res) => this.zone.run(() => {
        this.monthlyLoading = false;
        if (res.success && res.data) this.monthly = res.data; else this.monthlyError = res.message || 'Failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.monthlyLoading = false; this.monthlyError = err?.error?.message || 'Failed.'; this.cdr.detectChanges(); })
    });
  }

  loadExceptions(): void {
    this.exLoading = true; this.exError = '';
    this.svc.getExceptions(this.exFrom, this.exTo, this.exType).subscribe({
      next: (res) => this.zone.run(() => {
        this.exLoading = false;
        if (res.success && res.data) this.exceptions = res.data; else this.exError = res.message || 'Failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.exLoading = false; this.exError = err?.error?.message || 'Failed.'; this.cdr.detectChanges(); })
    });
  }

  statusLabel(s: string): string { return s.replace(/([a-z])([A-Z])/g, '$1 $2'); }
  statusClass(s: string): string { return 'st-' + s.toLowerCase(); }
}
