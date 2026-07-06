import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AccountingService } from '../../../services/accounting.service';
import { AuthService } from '../../../services/auth.service';
import { ACCOUNT_TYPES, AccountDto } from '../../../models/accounting.models';

@Component({
  selector: 'app-chart-of-accounts',
  standalone: false,
  templateUrl: './chart-of-accounts.component.html',
  styleUrl: './chart-of-accounts.component.scss'
})
export class ChartOfAccountsComponent implements OnInit {
  accounts: AccountDto[] = [];
  loading = false;
  filterType: string | null = null;
  search = '';
  includeInactive = false;
  searchTimer: any = null;

  readonly accountTypes = ACCOUNT_TYPES;
  canManage = false;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  editingIsSystem = false;
  form!: FormGroup;

  deleteDialogVisible = false;
  deleting: AccountDto | null = null;
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
    this.canManage = this.auth.hasPermission('Accounting.ManageAccounts');
    this.form = this.fb.group({
      code: ['', [Validators.required, Validators.maxLength(30)]],
      name: ['', [Validators.required, Validators.maxLength(150)]],
      accountType: ['Asset', Validators.required],
      isGroup: [false],
      parentAccountId: [null as number | null],
      isActive: [true],
      description: ['', Validators.maxLength(500)],
      requiresCostCenter: [false]   // Phase A3
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getAccounts(this.filterType ?? undefined, this.includeInactive, undefined, this.search.trim() || undefined)
      .subscribe({
        next: (res) => this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) this.accounts = res.data;
          this.cdr.detectChanges();
        }),
        error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
      });
  }

  onSearch(v: string): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.search = v; this.load(); }, 350);
  }

  // group accounts of the type currently chosen in the form — valid parents
  get parentOptions(): AccountDto[] {
    const t = this.form?.get('accountType')?.value;
    return this.accounts.filter(a => a.isGroup && a.accountType === t && a.id !== this.editingId);
  }

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.editingIsSystem = false;
    this.dialogError = '';
    this.form.reset({ code: '', name: '', accountType: 'Asset', isGroup: false, parentAccountId: null, isActive: true, description: '', requiresCostCenter: false });
    this.form.get('code')?.enable();
    this.form.get('accountType')?.enable();
    this.form.get('isGroup')?.enable();
    this.dialogVisible = true;
  }

  openEdit(a: AccountDto): void {
    this.dialogMode = 'edit';
    this.editingId = a.id;
    this.editingIsSystem = a.isSystem;
    this.dialogError = '';
    this.form.reset({
      code: a.code, name: a.name, accountType: a.accountType, isGroup: a.isGroup,
      parentAccountId: a.parentAccountId, isActive: a.isActive, description: a.description ?? '',
      requiresCostCenter: a.requiresCostCenter
    });
    // System accounts: lock code/type/grouping (backend enforces too)
    if (a.isSystem) { this.form.get('code')?.disable(); this.form.get('accountType')?.disable(); this.form.get('isGroup')?.disable(); }
    else { this.form.get('code')?.enable(); this.form.get('accountType')?.enable(); this.form.get('isGroup')?.enable(); }
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = {
      code: (v.code as string).trim(),
      name: (v.name as string).trim(),
      accountType: v.accountType,
      isGroup: !!v.isGroup,
      parentAccountId: v.parentAccountId ?? null,
      description: (v.description as string)?.trim() || null,
      requiresCostCenter: !v.isGroup && !!v.requiresCostCenter   // Phase A3 — detail accounts only
    };
    const done = (res: any) => this.zone.run(() => {
      this.dialogSaving = false;
      if (res.success) { this.dialogVisible = false; this.load(); }
      else this.dialogError = res.message || 'Save failed.';
      this.cdr.detectChanges();
    });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });

    if (this.dialogMode === 'create') this.svc.createAccount(base).subscribe({ next: done, error: err });
    else this.svc.updateAccount(this.editingId!, { id: this.editingId!, isActive: !!v.isActive, ...base }).subscribe({ next: done, error: err });
  }

  confirmDelete(a: AccountDto): void { this.deleting = a; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.deleteAccount(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleteBusy = false;
        if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); }
        else this.deleteError = res.message || 'Delete failed.';
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }

  typeClass(t: string): string { return t.toLowerCase(); }
}
