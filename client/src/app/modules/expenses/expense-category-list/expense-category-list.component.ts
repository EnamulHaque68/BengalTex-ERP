import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ExpenseService } from '../../../services/expense.service';
import { AccountingService } from '../../../services/accounting.service';
import { AuthService } from '../../../services/auth.service';
import { ExpenseCategoryDto } from '../../../models/expense.models';
import { AccountDto } from '../../../models/accounting.models';

@Component({
  selector: 'app-expense-category-list',
  standalone: false,
  templateUrl: './expense-category-list.component.html',
  styleUrl: './expense-category-list.component.scss'
})
export class ExpenseCategoryListComponent implements OnInit {
  categories: ExpenseCategoryDto[] = [];
  expenseAccounts: AccountDto[] = [];
  loading = false;
  includeInactive = false;
  canManage = false;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deleting: ExpenseCategoryDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(
    private svc: ExpenseService,
    private accounting: AccountingService,
    private auth: AuthService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('Expenses.ManageCategories');
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      ledgerAccountId: [null as number | null],
      isActive: [true],
      description: ['', Validators.maxLength(500)]
    });
    this.accounting.getAccounts('Expense', false, true).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.expenseAccounts = res.data; this.cdr.detectChanges(); })
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getCategories(this.includeInactive).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.categories = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({ name: '', ledgerAccountId: null, isActive: true, description: '' });
    this.dialogVisible = true;
  }
  openEdit(c: ExpenseCategoryDto): void {
    this.dialogMode = 'edit'; this.editingId = c.id; this.dialogError = '';
    this.form.reset({ name: c.name, ledgerAccountId: c.ledgerAccountId, isActive: c.isActive, description: c.description ?? '' });
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = { name: (v.name as string).trim(), ledgerAccountId: v.ledgerAccountId ?? null, description: (v.description as string)?.trim() || null };
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.createCategory(base).subscribe({ next: done, error: err });
    else this.svc.updateCategory(this.editingId!, { id: this.editingId!, isActive: !!v.isActive, ...base }).subscribe({ next: done, error: err });
  }

  confirmDelete(c: ExpenseCategoryDto): void { this.deleting = c; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.deleteCategory(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }
}
