import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService, FinancialYearDto, AccountingPeriodDto, YearClosePreviewDto, GrIrInitPreviewDto } from '../../../services/accounting.service';

import { apiErrorMessage } from '../../../shared/utils/http-error.util';
/**
 * Phase A1 — Fiscal Years & Accounting Periods. Create a year (12 auto-generated monthly
 * periods), drive each period's Open → Soft-closed → Locked lifecycle, run the year-end
 * close (with a preview of the net P&L swept to Retained Earnings) and audited reopen.
 */
@Component({
  selector: 'app-fiscal-year-list',
  standalone: false,
  templateUrl: './fiscal-year-list.component.html',
  styleUrl: './fiscal-year-list.component.scss'
})
export class FiscalYearListComponent implements OnInit {
  years: FinancialYearDto[] = [];
  loading = false;
  actionError = '';
  actionMessage = '';

  // Create dialog
  createVisible = false;
  creating = false;
  createError = '';
  newCode = '';
  newStartDate = '';
  newNotes = '';

  // Close-year dialog
  closeVisible = false;
  closing = false;
  closeError = '';
  closeTarget: FinancialYearDto | null = null;
  closePreview: YearClosePreviewDto | null = null;

  // Reopen dialog
  reopenVisible = false;
  reopening = false;
  reopenError = '';
  reopenTarget: FinancialYearDto | null = null;
  reopenReason = '';

  periodActionId: number | null = null;

  // Phase A2 — GR/IR initialization banner
  grIrPreview: GrIrInitPreviewDto | null = null;
  grIrVisible = false;
  grIrRunning = false;
  grIrError = '';
  grIrDate = '';

  constructor(
    private svc: AccountingService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load();
    this.loadGrIrPreview();
  }

  // ── Phase A2 — GR/IR initialization ──
  loadGrIrPreview(): void {
    this.svc.grIrInitPreview().subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.grIrPreview = res.data;
        this.cdr.detectChanges();
      }),
      error: () => { /* silently skip the banner if not permitted */ }
    });
  }

  get showGrIrBanner(): boolean {
    return !!this.grIrPreview && !this.grIrPreview.alreadyInitialized && this.grIrPreview.totalUnbilledValue > 0;
  }

  openGrIr(): void {
    this.grIrDate = new Date().toISOString().slice(0, 10);
    this.grIrError = '';
    this.grIrVisible = true;
  }

  runGrIr(): void {
    if (this.grIrRunning) return;
    this.grIrRunning = true;
    this.grIrError = '';
    this.svc.initializeGrIr(this.grIrDate).subscribe({
      next: (res) => this.zone.run(() => {
        this.grIrRunning = false;
        if (res.success) { this.grIrVisible = false; this.actionMessage = res.message || 'GR/IR initialized.'; this.loadGrIrPreview(); }
        else this.grIrError = res.message || 'Initialization failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.grIrRunning = false;
        this.grIrError = apiErrorMessage(err, 'Initialization failed.');
        this.cdr.detectChanges();
      })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getFinancialYears().subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.years = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  // ── Create year ──
  openCreate(): void {
    const now = new Date();
    this.newCode = `FY${now.getFullYear()}`;
    this.newStartDate = `${now.getFullYear()}-01-01`;
    this.newNotes = '';
    this.createError = '';
    this.createVisible = true;
  }

  create(): void {
    if (this.creating || !this.newCode.trim() || !this.newStartDate) return;
    this.creating = true;
    this.createError = '';
    this.svc.createFinancialYear({
      code: this.newCode.trim(), startDate: this.newStartDate, notes: this.newNotes.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.creating = false;
        if (res.success) { this.createVisible = false; this.load(); }
        else this.createError = res.message || 'Could not create the year.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.creating = false;
        this.createError = apiErrorMessage(err, 'Could not create the year.');
        this.cdr.detectChanges();
      })
    });
  }

  // ── Period lifecycle ──
  changePeriod(p: AccountingPeriodDto, action: 'soft-close' | 'lock' | 'reopen'): void {
    if (this.periodActionId) return;
    this.periodActionId = p.id;
    this.actionError = '';
    this.svc.changePeriodStatus(p.id, action).subscribe({
      next: (res) => this.zone.run(() => {
        this.periodActionId = null;
        if (res.success) { this.actionMessage = res.message || 'Done.'; this.load(); }
        else this.actionError = res.message || 'Action failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.periodActionId = null;
        this.actionError = apiErrorMessage(err, 'Action failed.');
        this.cdr.detectChanges();
      })
    });
  }

  /** All 12 periods locked → the year is closable. */
  canClose(y: FinancialYearDto): boolean {
    return y.status === 'Open' && y.periods.length > 0 && y.periods.every(p => p.status === 'Locked');
  }

  // ── Year close ──
  openClose(y: FinancialYearDto): void {
    this.closeTarget = y;
    this.closePreview = null;
    this.closeError = '';
    this.closeVisible = true;
    this.svc.yearClosePreview(y.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.closePreview = res.data;
        else this.closeError = res.message || 'Could not compute the preview.';
        this.cdr.detectChanges();
      })
    });
  }

  confirmClose(): void {
    if (!this.closeTarget || this.closing) return;
    this.closing = true;
    this.closeError = '';
    this.svc.closeYear(this.closeTarget.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.closing = false;
        if (res.success) { this.closeVisible = false; this.actionMessage = res.message || 'Year closed.'; this.load(); }
        else this.closeError = res.message || 'Close failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.closing = false;
        this.closeError = apiErrorMessage(err, 'Close failed.');
        this.cdr.detectChanges();
      })
    });
  }

  // ── Reopen ──
  openReopen(y: FinancialYearDto): void {
    this.reopenTarget = y;
    this.reopenReason = '';
    this.reopenError = '';
    this.reopenVisible = true;
  }

  confirmReopen(): void {
    if (!this.reopenTarget || this.reopening || !this.reopenReason.trim()) return;
    this.reopening = true;
    this.reopenError = '';
    this.svc.reopenYear(this.reopenTarget.id, this.reopenReason.trim()).subscribe({
      next: (res) => this.zone.run(() => {
        this.reopening = false;
        if (res.success) { this.reopenVisible = false; this.actionMessage = res.message || 'Year reopened.'; this.load(); }
        else this.reopenError = res.message || 'Reopen failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.reopening = false;
        this.reopenError = apiErrorMessage(err, 'Reopen failed.');
        this.cdr.detectChanges();
      })
    });
  }

  periodClass(status: string): string {
    switch (status) {
      case 'Open': return 'p-open';
      case 'SoftClosed': return 'p-soft';
      case 'Locked': return 'p-locked';
      default: return '';
    }
  }

  formatMoney(v: number | undefined): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 })
      .format(v || 0);
  }
}
