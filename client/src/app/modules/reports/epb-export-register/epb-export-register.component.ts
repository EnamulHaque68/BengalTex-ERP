import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import { EpbExportRegisterReportDto, EpbExportRegisterRowDto } from '../../../models/reports.models';

@Component({
  selector: 'app-epb-export-register',
  standalone: false,
  templateUrl: './epb-export-register.component.html',
  styleUrl: './epb-export-register.component.scss'
})
export class EpbExportRegisterComponent implements OnInit {
  report: EpbExportRegisterReportDto | null = null;
  loading = false;
  error = '';

  fromDate: string = '';
  toDate: string = '';
  pendingFormExpOnly = false;

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

  run(): void {
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();
    this.svc.getEpbExportRegister(this.fromDate || undefined, this.toDate || undefined, this.pendingFormExpOnly).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.report = res.data;
        else { this.report = null; this.error = res.message || 'Failed.'; }
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => {
        this.loading = false;
        this.error = e?.error?.message || 'Failed.';
        this.cdr.detectChanges();
      })
    });
  }

  formatCurrency(amount: number, code = 'BDT'): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: code, maximumFractionDigits: 2 }).format(amount || 0);
  }

  formatNumber(n: number): string {
    return new Intl.NumberFormat('en-BD', { maximumFractionDigits: 2 }).format(n || 0);
  }

  downloadCsv(): void {
    if (!this.report || this.report.rows.length === 0) return;
    const header = [
      'Invoice #', 'Invoice Date', 'Shipment Date', 'Form-EXP #', 'LC #',
      'Customer Code', 'Customer Name', 'Country', 'Sales Order #',
      'Currency', 'Exchange Rate',
      'FOB (Foreign)', 'FOB (BDT)', 'Total (Foreign)', 'Total (BDT)',
      'Status', 'HS Codes'
    ];
    const escape = (s: any): string => {
      if (s == null) return '';
      const str = String(s);
      if (str.includes(',') || str.includes('"') || str.includes('\n')) {
        return `"${str.replace(/"/g, '""')}"`;
      }
      return str;
    };
    const rows = this.report.rows.map((r: EpbExportRegisterRowDto) => [
      r.invoiceCode, r.invoiceDate, r.shipmentDate ?? '', r.epbFormNumber ?? '', r.lcNumber ?? '',
      r.customerCode, r.customerName, r.countryOfDestination, r.salesOrderCode,
      r.currencyCode, r.exchangeRate.toFixed(6),
      r.fobAmountForeign.toFixed(2), r.fobAmountBdt.toFixed(2),
      r.totalAmountForeign.toFixed(2), r.totalAmountBdt.toFixed(2),
      r.status, r.hsCodesSummary ?? ''
    ].map(escape).join(','));
    const csv = [header.join(','), ...rows].join('\r\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `epb-export-register-${this.fromDate}-to-${this.toDate}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}
