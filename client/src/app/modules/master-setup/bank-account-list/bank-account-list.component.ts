import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MasterSetupService } from '../../../services/master-setup.service';
import { AccountingService } from '../../../services/accounting.service';
import { AuthService } from '../../../services/auth.service';
import { BankAccountDto, BankAccountType, BANK_ACCOUNT_TYPES } from '../../../models/master-setup.models';

@Component({
  selector: 'app-bank-account-list',
  standalone: false,
  templateUrl: './bank-account-list.component.html',
  styleUrl: './bank-account-list.component.scss'
})
export class BankAccountListComponent implements OnInit {
  items: BankAccountDto[] = [];
  loading = false;
  includeInactive = false;
  canManage = false;

  readonly accountTypes = BANK_ACCOUNT_TYPES;
  ledgerAccounts: any[] = [];

  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deleting: BankAccountDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(private svc: MasterSetupService, private accountingSvc: AccountingService,
              private auth: AuthService, private fb: FormBuilder,
              private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('MasterSetup.ManageBankAccounts');
    this.form = this.fb.group({
      accountName: ['', [Validators.required, Validators.maxLength(200)]],
      bankName: ['', [Validators.required, Validators.maxLength(150)]],
      branchName: ['', Validators.maxLength(150)],
      accountNumber: ['', [Validators.required, Validators.maxLength(50)]],
      accountType: ['Current' as BankAccountType, Validators.required],
      routingNumber: ['', Validators.maxLength(30)],
      swiftCode: ['', Validators.maxLength(20)],
      currency: ['BDT', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
      ledgerAccountId: [null as number | null],
      notes: ['', Validators.maxLength(1000)],
      isActive: [true]
    });
    // Fetch postable Bank-type accounts from Chart of Accounts
    this.accountingSvc.getAccounts('Asset', false, true).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          // Show all postable asset accounts; user picks the relevant Bank node
          this.ledgerAccounts = res.data;
        }
        this.cdr.detectChanges();
      })
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getBankAccounts(this.includeInactive).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.items = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({
      accountName: '', bankName: '', branchName: '', accountNumber: '',
      accountType: 'Current', routingNumber: '', swiftCode: '', currency: 'BDT',
      ledgerAccountId: null, notes: '', isActive: true
    });
    this.dialogVisible = true;
  }
  openEdit(b: BankAccountDto): void {
    this.dialogMode = 'edit'; this.editingId = b.id; this.dialogError = '';
    this.form.reset({
      accountName: b.accountName, bankName: b.bankName,
      branchName: b.branchName ?? '', accountNumber: b.accountNumber,
      accountType: b.accountType, routingNumber: b.routingNumber ?? '',
      swiftCode: b.swiftCode ?? '', currency: b.currency,
      ledgerAccountId: b.ledgerAccountId, notes: b.notes ?? '', isActive: b.isActive
    });
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = {
      accountName: v.accountName.trim(), bankName: v.bankName.trim(),
      branchName: (v.branchName as string)?.trim() || null,
      accountNumber: v.accountNumber.trim(), accountType: v.accountType,
      routingNumber: (v.routingNumber as string)?.trim() || null,
      swiftCode: (v.swiftCode as string)?.trim() || null,
      currency: v.currency.trim().toUpperCase(), ledgerAccountId: v.ledgerAccountId,
      notes: (v.notes as string)?.trim() || null
    };
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.createBankAccount(base).subscribe({ next: done, error: err });
    else this.svc.updateBankAccount(this.editingId!, { ...base, isActive: v.isActive }).subscribe({ next: done, error: err });
  }

  confirmDelete(b: BankAccountDto): void { this.deleting = b; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.deleteBankAccount(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }
}
