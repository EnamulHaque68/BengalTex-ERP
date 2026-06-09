import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import {
  OperatorProductivityReportDto, MachineProductivityReportDto
} from '../../../models/reports.models';

type ViewMode = 'operator' | 'machine';

@Component({
  selector: 'app-productivity-report',
  standalone: false,
  templateUrl: './productivity-report.component.html',
  styleUrl: './productivity-report.component.scss'
})
export class ProductivityReportComponent implements OnInit {
  view: ViewMode = 'operator';
  fromDate: string = '';
  toDate: string = '';
  loading = false;
  error = '';

  operatorReport: OperatorProductivityReportDto | null = null;
  machineReport: MachineProductivityReportDto | null = null;

  constructor(
    private svc: ReportsService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const now = new Date();
    const thirtyAgo = new Date(now);
    thirtyAgo.setDate(now.getDate() - 30);
    this.toDate = now.toISOString().slice(0, 10);
    this.fromDate = thirtyAgo.toISOString().slice(0, 10);
    this.run();
  }

  switchView(v: ViewMode): void {
    if (this.view === v) return;
    this.view = v;
    this.run();
  }

  run(): void {
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();
    if (this.view === 'operator') {
      this.svc.getOperatorProductivity(this.fromDate || undefined, this.toDate || undefined).subscribe({
        next: (res) => this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) this.operatorReport = res.data;
          else { this.operatorReport = null; this.error = res.message || 'Failed.'; }
          this.cdr.detectChanges();
        }),
        error: (e) => this.zone.run(() => {
          this.loading = false;
          this.error = e?.error?.message || 'Failed.';
          this.cdr.detectChanges();
        })
      });
    } else {
      this.svc.getMachineProductivity(this.fromDate || undefined, this.toDate || undefined).subscribe({
        next: (res) => this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) this.machineReport = res.data;
          else { this.machineReport = null; this.error = res.message || 'Failed.'; }
          this.cdr.detectChanges();
        }),
        error: (e) => this.zone.run(() => {
          this.loading = false;
          this.error = e?.error?.message || 'Failed.';
          this.cdr.detectChanges();
        })
      });
    }
  }

  formatNumber(n: number): string {
    return new Intl.NumberFormat('en-BD', { maximumFractionDigits: 2 }).format(n || 0);
  }

  hoursFromMinutes(min: number): string {
    if (!min || min === 0) return '0';
    const h = min / 60;
    return h.toFixed(1);
  }

  rejectRateClass(p: number): string {
    if (p >= 10) return 'bad';
    if (p >= 5) return 'warn';
    return 'good';
  }

  uphClass(uph: number, avg: number): string {
    if (avg === 0) return '';
    if (uph >= avg * 1.2) return 'top';
    if (uph < avg * 0.8) return 'low';
    return '';
  }
}
