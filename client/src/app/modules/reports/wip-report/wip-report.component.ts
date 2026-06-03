import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import { WipReportDto } from '../../../models/reports.models';

@Component({
  selector: 'app-wip-report',
  standalone: false,
  templateUrl: './wip-report.component.html',
  styleUrl: './wip-report.component.scss'
})
export class WipReportComponent implements OnInit {
  report: WipReportDto | null = null;
  loading = false;
  error = '';

  constructor(
    private reportsService: ReportsService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();
    this.reportsService.getWip().subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.report = res.data;
        else { this.report = null; this.error = res.message || 'Failed to load report.'; }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loading = false;
        this.error = err?.error?.message || 'Failed to load report.';
        this.cdr.detectChanges();
      })
    });
  }

  formatNumber(n: number): string {
    return new Intl.NumberFormat('en-BD', { maximumFractionDigits: 4 }).format(n || 0);
  }
}
