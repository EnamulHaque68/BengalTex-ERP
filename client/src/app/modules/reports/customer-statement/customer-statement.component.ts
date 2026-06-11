import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import { CustomerService } from '../../../services/customer.service';
import { CustomerStatementLineDto, CustomerStatementReportDto } from '../../../models/reports.models';
import { CustomerListItemDto } from '../../../models/customer.models';

interface StatementEmailForm {
  toAddresses: string;
  ccAddresses: string;
  subject: string;
  htmlBody: string;
}

@Component({
  selector: 'app-customer-statement',
  standalone: false,
  templateUrl: './customer-statement.component.html',
  styleUrl: './customer-statement.component.scss'
})
export class CustomerStatementComponent implements OnInit {
  report: CustomerStatementReportDto | null = null;
  loading = false;
  error = '';

  customers: CustomerListItemDto[] = [];
  customerId: number | null = null;
  fromDate = '';
  toDate = '';

  constructor(
    private svc: ReportsService,
    private customerSvc: CustomerService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const now = new Date();
    const threeMonthsAgo = new Date(now);
    threeMonthsAgo.setMonth(now.getMonth() - 3);
    this.toDate = now.toISOString().slice(0, 10);
    this.fromDate = threeMonthsAgo.toISOString().slice(0, 10);

    this.customerSvc.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.customers = res.data.items;
        this.cdr.detectChanges();
      })
    });
  }

  run(): void {
    if (!this.customerId) { this.error = 'Pick a customer first.'; return; }
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();
    this.svc.getCustomerStatement(this.customerId, this.fromDate || undefined, this.toDate || undefined).subscribe({
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
      toAddresses: this.report.customerEmail ?? '',
      ccAddresses: '',
      subject: `Statement of Account — ${this.report.customerCode} (${this.report.fromDate} to ${this.report.toDate})`,
      htmlBody:
        `<p>Dear ${this.report.customerName},</p>` +
        `<p>Please find attached your statement of account for the period <strong>${this.report.fromDate}</strong> to <strong>${this.report.toDate}</strong>.</p>` +
        `<p>Closing balance: <strong>${this.formatCurrency(this.report.closingBalance)}</strong>.</p>` +
        `<p>Kindly reconcile and report any discrepancy within 7 days.</p>` +
        `<p>Regards,<br/>Accounts Team</p>`
    };
    this.emailDlgVisible = true;
  }

  sendStatementEmail(): void {
    if (!this.report || this.emailSending) return;
    this.emailSending = true;
    this.emailError = '';
    this.cdr.detectChanges();
    this.svc.emailCustomerStatement({
      partyId: this.report.customerId,
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
    const rows = this.report.lines.map((l: CustomerStatementLineDto) => [
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
    a.download = `customer-statement-${this.report.customerCode}-${this.fromDate}-to-${this.toDate}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}
