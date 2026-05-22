import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AccountingService } from '../../../services/accounting.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  JOURNAL_STATUSES, AccountDto, JournalEntryDto, JournalEntryListItemDto
} from '../../../models/accounting.models';

@Component({
  selector: 'app-journal-entry-list',
  standalone: false,
  templateUrl: './journal-entry-list.component.html',
  styleUrl: './journal-entry-list.component.scss'
})
export class JournalEntryListComponent implements OnInit {
  entries: JournalEntryListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  fromDate: string | null = null;
  toDate: string | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;
  actionError = '';
  rowActionId: number | null = null;

  readonly statuses = JOURNAL_STATUSES;
  postableAccounts: AccountDto[] = [];

  canCreate = false;
  canPost = false;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deleting: JournalEntryListItemDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(
    private svc: AccountingService,
    private auth: AuthService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canCreate = this.auth.hasPermission('Accounting.CreateJournal');
    this.canPost = this.auth.hasPermission('Accounting.PostJournal');
    this.form = this.fb.group({
      entryDate: [this.todayIso(), Validators.required],
      reference: ['', Validators.maxLength(100)],
      narration: ['', Validators.maxLength(1000)],
      lines: this.fb.array([])
    });
    this.loadAccounts();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }
  get lines(): FormArray { return this.form.get('lines') as FormArray; }

  private newLine(accountId: number | null = null, debit = 0, credit = 0, narration = '') {
    return this.fb.group({
      accountId: [accountId, Validators.required],
      debit: [debit], credit: [credit], lineNarration: [narration]
    });
  }
  addLine(): void { this.lines.push(this.newLine()); }
  removeLine(i: number): void { this.lines.removeAt(i); }

  onDebit(i: number, val: any): void { if (Number(val) > 0) this.lines.at(i).get('credit')?.setValue(0); }
  onCredit(i: number, val: any): void { if (Number(val) > 0) this.lines.at(i).get('debit')?.setValue(0); }

  get totalDebit(): number { return (this.lines.getRawValue() as any[]).reduce((s, l) => s + (Number(l.debit) || 0), 0); }
  get totalCredit(): number { return (this.lines.getRawValue() as any[]).reduce((s, l) => s + (Number(l.credit) || 0), 0); }
  get balanced(): boolean { return this.totalDebit > 0 && Math.abs(this.totalDebit - this.totalCredit) < 0.005; }

  private loadAccounts(): void {
    this.svc.getAccounts(undefined, false, true).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.postableAccounts = res.data; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getJournals(this.parameters, this.filterStatus ?? undefined, this.fromDate ?? undefined, this.toDate ?? undefined)
      .subscribe({
        next: (res) => this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) { this.entries = res.data.items; this.totalCount = res.data.totalCount; }
          this.cdr.detectChanges();
        }),
        error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
      });
  }

  onSearch(v: string): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.parameters.search = v; this.parameters.page = 1; this.load(); }, 350);
  }
  onPage(e: any): void { this.parameters.page = Math.floor(e.first / e.rows) + 1; this.parameters.pageSize = e.rows; this.load(); }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.lines.clear();
    this.form.reset({ entryDate: this.todayIso(), reference: '', narration: '' });
    this.addLine(); this.addLine();
    this.form.enable();
    this.dialogVisible = true;
  }

  open(e: JournalEntryListItemDto): void {
    this.editingId = e.id; this.dialogError = ''; this.dialogVisible = true;
    this.lines.clear(); this.form.enable();
    this.svc.getJournal(e.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const j = res.data;
          this.dialogMode = j.status === 'Draft' ? 'edit' : 'view';
          this.form.patchValue({ entryDate: j.entryDate, reference: j.reference ?? '', narration: j.narration ?? '' });
          for (const l of j.lines) this.lines.push(this.newLine(l.accountId, l.debit, l.credit, l.lineNarration ?? ''));
          if (this.dialogMode === 'view') this.form.disable();
          this.cdr.detectChanges();
        }
      })
    });
  }

  save(): void {
    if (this.dialogSaving || this.dialogMode === 'view') return;
    if (!this.balanced) { this.dialogError = 'Total debits must equal total credits (and be > 0).'; return; }
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const payload = {
      entryDate: v.entryDate, reference: (v.reference as string)?.trim() || null, narration: (v.narration as string)?.trim() || null,
      lines: (v.lines as any[]).map(l => ({ accountId: l.accountId, debit: Number(l.debit) || 0, credit: Number(l.credit) || 0, lineNarration: (l.lineNarration as string)?.trim() || null }))
    };
    const done = (res: any) => this.zone.run(() => {
      this.dialogSaving = false;
      if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.';
      this.cdr.detectChanges();
    });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.createJournal(payload).subscribe({ next: done, error: err });
    else this.svc.updateJournal(this.editingId!, { id: this.editingId!, ...payload }).subscribe({ next: done, error: err });
  }

  post(e: JournalEntryListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = e.id; this.actionError = ''; this.cdr.detectChanges();
    this.svc.postJournal(e.id).subscribe({
      next: (res) => this.zone.run(() => { this.rowActionId = null; if (res.success) this.load(); else this.actionError = res.message || 'Post failed.'; this.cdr.detectChanges(); }),
      error: (er) => this.zone.run(() => { this.rowActionId = null; this.actionError = er?.error?.message || 'Post failed.'; this.cdr.detectChanges(); })
    });
  }

  confirmDelete(e: JournalEntryListItemDto): void { this.deleting = e; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.deleteJournal(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (er) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = er?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }

  meta(c: any) { return c.getRawValue(); }
}
