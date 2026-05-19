import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import { VatSummaryReportDto } from '../../../models/reports.models';

@Component({
  selector: 'app-vat-summary',
  standalone: false,
  templateUrl: './vat-summary.component.html',
  styleUrl: './vat-summary.component.scss'
})
export class VatSummaryComponent implements OnInit {

  report: VatSummaryReportDto | null = null;
  loading = false;
  error = '';

  fromDate: string = '';
  toDate: string = '';

  constructor(
    private reportsService: ReportsService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const today = new Date();
    // Default = current calendar month
    const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
    const monthEnd = new Date(today.getFullYear(), today.getMonth() + 1, 0);
    this.fromDate = monthStart.toISOString().slice(0, 10);
    this.toDate = monthEnd.toISOString().slice(0, 10);
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();

    this.reportsService.getVatSummary(
      this.fromDate || undefined,
      this.toDate || undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.report = res.data;
          } else {
            this.error = res.message || 'Failed to load report.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.loading = false;
          this.error = err?.error?.message || 'Failed to load report.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency', currency: 'BDT', maximumFractionDigits: 2
    }).format(amount || 0);
  }

  print(): void {
    window.print();
  }
}
