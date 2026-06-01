import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BankReconciliationService } from '../../../services/bank-reconciliation.service';
import { AuthService } from '../../../services/auth.service';
import {
  BankStatementDto, BankStatementLineDto, UnmatchedJournalLineDto
} from '../../../models/bank-reconciliation.models';

@Component({
  selector: 'app-reconciliation-workspace',
  standalone: false,
  templateUrl: './reconciliation-workspace.component.html',
  styleUrl: './reconciliation-workspace.component.scss'
})
export class ReconciliationWorkspaceComponent implements OnInit {

  statement: BankStatementDto | null = null;
  unmatched: UnmatchedJournalLineDto[] = [];
  loading = false;
  actionMessage = '';
  actionError = '';
  rowActionId: number | null = null;

  selectedStatementLine: BankStatementLineDto | null = null;
  canManage = false;

  // Add/edit line dialog
  lineDialogVisible = false;
  lineDialogMode: 'add' | 'edit' = 'add';
  lineSaving = false;
  lineError = '';
  editingLineId: number | null = null;
  lineForm!: FormGroup;

  // Exclude dialog
  excludeDialogVisible = false;
  excludeTarget: BankStatementLineDto | null = null;
  excludeNotes = '';
  excludeBusy = false;
  excludeError = '';

  constructor(
    private svc: BankReconciliationService,
    private auth: AuthService,
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('BankReconciliation.Manage');
    this.lineForm = this.fb.group({
      transactionDate: [this.todayIso(), Validators.required],
      description: ['', [Validators.required, Validators.maxLength(500)]],
      referenceNumber: ['', Validators.maxLength(100)],
      amount: [0, Validators.required],
      notes: ['', Validators.maxLength(1000)]
    });
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loadAll(id);
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  loadAll(id: number): void {
    this.loading = true;
    this.svc.getById(id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.statement = res.data;
        this.loading = false; this.cdr.detectChanges();
      })
    });
    this.svc.getUnmatchedJournalLines(id).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.unmatched = res.data; this.cdr.detectChanges(); })
    });
  }
  refresh(): void { if (this.statement) this.loadAll(this.statement.id); }

  back(): void { this.router.navigate(['/bank-reconciliation']); }

  selectStatementLine(l: BankStatementLineDto): void {
    if (!this.canManage || this.statement?.isReconciled) return;
    if (l.status === 'Matched' || l.status === 'Excluded') { this.selectedStatementLine = null; return; }
    this.selectedStatementLine = this.selectedStatementLine?.id === l.id ? null : l;
  }

  matchTo(jl: UnmatchedJournalLineDto): void {
    if (!this.selectedStatementLine || this.rowActionId) return;
    this.rowActionId = jl.id; this.actionError = ''; this.cdr.detectChanges();
    this.svc.match(this.selectedStatementLine.id, jl.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) {
          this.actionMessage = `Matched ${this.selectedStatementLine!.description} ↔ ${jl.journalEntryCode}`;
          this.selectedStatementLine = null;
          this.refresh();
        } else this.actionError = res.message || 'Match failed.';
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => { this.rowActionId = null; this.actionError = e?.error?.message || 'Match failed.'; this.cdr.detectChanges(); })
    });
  }

  unmatchLine(l: BankStatementLineDto): void {
    if (!this.canManage || this.statement?.isReconciled || this.rowActionId) return;
    this.rowActionId = l.id; this.cdr.detectChanges();
    this.svc.unmatch(l.id).subscribe({
      next: () => this.zone.run(() => { this.rowActionId = null; this.refresh(); }),
      error: () => this.zone.run(() => { this.rowActionId = null; this.cdr.detectChanges(); })
    });
  }

  openExclude(l: BankStatementLineDto): void { this.excludeTarget = l; this.excludeNotes = l.notes ?? ''; this.excludeError = ''; this.excludeDialogVisible = true; }
  doExclude(): void {
    if (!this.excludeTarget || this.excludeBusy) return;
    this.excludeBusy = true; this.excludeError = ''; this.cdr.detectChanges();
    this.svc.exclude(this.excludeTarget.id, this.excludeNotes.trim() || null).subscribe({
      next: (res) => this.zone.run(() => { this.excludeBusy = false; if (res.success) { this.excludeDialogVisible = false; this.excludeTarget = null; this.refresh(); } else this.excludeError = res.message || 'Exclude failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.excludeBusy = false; this.excludeError = e?.error?.message || 'Exclude failed.'; this.cdr.detectChanges(); })
    });
  }

  // ── Line CRUD ──
  openAddLine(): void {
    this.lineDialogMode = 'add'; this.editingLineId = null; this.lineError = '';
    this.lineForm.reset({ transactionDate: this.todayIso(), description: '', referenceNumber: '', amount: 0, notes: '' });
    this.lineDialogVisible = true;
  }
  openEditLine(l: BankStatementLineDto): void {
    this.lineDialogMode = 'edit'; this.editingLineId = l.id; this.lineError = '';
    this.lineForm.reset({
      transactionDate: l.transactionDate, description: l.description,
      referenceNumber: l.referenceNumber ?? '', amount: l.amount, notes: l.notes ?? ''
    });
    this.lineDialogVisible = true;
  }
  saveLine(): void {
    if (!this.statement || this.lineForm.invalid || this.lineSaving) return;
    this.lineSaving = true; this.lineError = ''; this.cdr.detectChanges();
    const v = this.lineForm.getRawValue();
    const base = {
      transactionDate: v.transactionDate, description: v.description.trim(),
      referenceNumber: (v.referenceNumber as string)?.trim() || null,
      amount: Number(v.amount) || 0, notes: (v.notes as string)?.trim() || null
    };
    const done = (res: any) => this.zone.run(() => { this.lineSaving = false; if (res.success) { this.lineDialogVisible = false; this.refresh(); } else this.lineError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.lineSaving = false; this.lineError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.lineDialogMode === 'add') this.svc.addLine(this.statement.id, base).subscribe({ next: done, error: err });
    else this.svc.updateLine(this.editingLineId!, base).subscribe({ next: done, error: err });
  }
  deleteLine(l: BankStatementLineDto): void {
    if (this.rowActionId) return;
    this.rowActionId = l.id; this.cdr.detectChanges();
    this.svc.deleteLine(l.id).subscribe({
      next: () => this.zone.run(() => { this.rowActionId = null; this.refresh(); }),
      error: () => this.zone.run(() => { this.rowActionId = null; this.cdr.detectChanges(); })
    });
  }

  reconcile(): void {
    if (!this.statement || this.rowActionId) return;
    this.rowActionId = this.statement.id; this.actionError = ''; this.cdr.detectChanges();
    this.svc.reconcile(this.statement.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) { this.actionMessage = 'Statement reconciled. 🎉'; this.refresh(); }
        else this.actionError = res.message || 'Reconciliation failed.';
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => { this.rowActionId = null; this.actionError = e?.error?.message || 'Reconciliation failed.'; this.cdr.detectChanges(); })
    });
  }

  // ── Helpers ──
  lineStatusSeverity(s: string): 'success' | 'warn' | 'secondary' {
    return s === 'Matched' ? 'success' : s === 'Excluded' ? 'secondary' : 'warn';
  }

  // For visual matching hint: highlight journal lines that match selected statement line's amount
  amountMatches(jl: UnmatchedJournalLineDto): boolean {
    if (!this.selectedStatementLine) return false;
    return Math.round(this.selectedStatementLine.amount * 100) === Math.round(jl.amount * 100);
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }
}
