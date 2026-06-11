import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import { SupplierService } from '../../../services/supplier.service';
import { SupplierStatementLineDto, SupplierStatementReportDto } from '../../../models/reports.models';
import { SupplierListItemDto } from '../../../models/supplier.models';

interface StatementEmailForm {
  toAddresses: string;
  ccAddresses: string;
  subject: string;
  htmlBody: string;
}

@Component({
  selector: 'app-supplier-statement',
  standalone: false,
  templateUrl: './supplier-statement.component.html',
  styleUrl: './supplier-statement.component.scss'
})
export class SupplierStatementComponent implements OnInit {
  report: SupplierStatementReportDto | null = null;
  loading = false;
  error = '';

  suppliers: SupplierListItemDto[] = [];
  supplierId: number | null = null;
  fromDate = '';
  toDate = '';

  constructor(
    private svc: ReportsService,
    private supplierSvc: SupplierService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const now = new Date();
    const threeMonthsAgo = new Date(now);
    threeMonthsAgo.setMonth(now.getMonth() - 3);
    this.toDate = now.toISOString().slice(0, 10);
    this.fromDate = threeMonthsAgo.toISOString().slice(0, 10);

    this.supplierSvc.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.suppliers = res.data.items;
        this.cdr.detectChanges();
      })
    });
  }

  run(): void {
    if (!this.supplierId) { this.error = 'Pick a supplier first.'; return; }
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();
    this.svc.getSupplierStatement(this.supplierId, this.fromDate || undefined, this.toDate || undefined).subscribe({
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

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }

  print(): void { window.print(); }

  // ─── Email statement (PDF attached) ────────────────────────────────────
  emailDlgVisible = false;
  emailSending = false;
  emailError = '';
  emailForm: StatementEmailForm = { toAddresses: '', ccAddresses: '', subject: '', htmlBody: '' };

  openEmailStatement(): void {
    if (!this.report) return;
    this.emailError = '';
    this.emailForm = {
      toAddresses: this.report.supplierEmail ?? '',
      ccAddresses: '',
      subject: `Payable Statement — ${this.report.supplierCode} (${this.report.fromDate} to ${this.report.toDate})`,
      htmlBody:
        `<p>Dear ${this.report.supplierName},</p>` +
        `<p>Please find attached our statement of account with you for the period <strong>${this.report.fromDate}</strong> to <strong>${this.report.toDate}</strong>.</p>` +
        `<p>Closing payable per our records: <strong>${this.formatCurrency(this.report.closingBalance)}</strong>.</p>` +
        `<p>Kindly reconcile against your ledger and report any discrepancy within 7 days.</p>` +
        `<p>Regards,<br/>Accounts Team</p>`
    };
    this.emailDlgVisible = true;
  }

  sendStatementEmail(): void {
    if (!this.report || this.emailSending) return;
    this.emailSending = true;
    this.emailError = '';
    this.cdr.detectChanges();
    this.svc.emailSupplierStatement({
      partyId: this.report.supplierId,
      fromDate: this.fromDate || null,
      toDate: this.toDate || null,
      toAddresses: this.emailForm.toAddresses.trim(),
      ccAddresses: this.emailForm.ccAddresses.trim() || null,
      subject: this.emailForm.subject.trim(),
      htmlBody: this.emailForm.htmlBody
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.emailSending = false;
        if (res.success) this.emailDlgVisible = false;
        else this.emailError = res.message || 'Send failed.';
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => {
        this.emailSending = false;
        this.emailError = e?.error?.message || 'Send failed.';
        this.cdr.detectChanges();
      })
    });
  }

  downloadCsv(): void {
    if (!this.report || this.report.lines.length === 0) return;
    const header = ['Date', 'Type', 'Reference', 'Document', 'Debit (BDT)', 'Credit (BDT)', 'Running Balance (BDT)'];
    const escape = (s: any): string => {
      if (s == null) return '';
      const str = String(s);
      if (str.includes(',') || str.includes('"') || str.includes('\n'))
        return `"${str.replace(/"/g, '""')}"`;
      return str;
    };
    const openingRow = ['', 'Opening', 'Brought forward', '', '', '', this.report.openingBalance.toFixed(2)];
    const rows = this.report.lines.map((l: SupplierStatementLineDto) => [
      l.date, l.type, l.reference, l.documentRef ?? '',
      l.debit > 0 ? l.debit.toFixed(2) : '',
      l.credit > 0 ? l.credit.toFixed(2) : '',
      l.runningBalance.toFixed(2)
    ]);
    const closingRow = ['', 'Closing', 'Carried forward', '',
      this.report.totalDebits.toFixed(2),
      this.report.totalCredits.toFixed(2),
      this.report.closingBalance.toFixed(2)];
    const csv = [header.join(','), [openingRow, ...rows, closingRow].map(r => r.map(escape).join(','))].flat().join('\r\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `supplier-statement-${this.report.supplierCode}-${this.fromDate}-to-${this.toDate}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}
