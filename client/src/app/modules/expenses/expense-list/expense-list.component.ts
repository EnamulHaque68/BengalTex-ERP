import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ExpenseService } from '../../../services/expense.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  EXPENSE_PAYMENT_METHODS, EXPENSE_STATUSES,
  ExpenseCategoryDto, ExpenseListItemDto
} from '../../../models/expense.models';

@Component({
  selector: 'app-expense-list',
  standalone: false,
  templateUrl: './expense-list.component.html',
  styleUrl: './expense-list.component.scss'
})
export class ExpenseListComponent implements OnInit {
  expenses: ExpenseListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterCategoryId: number | null = null;
  filterStatus: string | null = null;
  fromDate: string | null = null;
  toDate: string | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;
  actionError = '';
  rowActionId: number | null = null;

  readonly methods = EXPENSE_PAYMENT_METHODS;
  readonly statuses = EXPENSE_STATUSES;
  categories: ExpenseCategoryDto[] = [];

  canCreate = false;
  canApprove = false;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deleting: ExpenseListItemDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(
    private svc: ExpenseService,
    private auth: AuthService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canCreate = this.auth.hasPermission('Expenses.Create');
    this.canApprove = this.auth.hasPermission('Expenses.Approve');
    this.form = this.fb.group({
      expenseDate: [this.todayIso(), Validators.required],
      expenseCategoryId: [null as number | null, Validators.required],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      paymentMethod: ['Cash', Validators.required],
      payee: ['', Validators.maxLength(200)],
      referenceNumber: ['', Validators.maxLength(100)],
      description: ['', Validators.maxLength(1000)]
    });
    this.loadCategories();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  private loadCategories(): void {
    this.svc.getCategories(false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.categories = res.data; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters, this.filterCategoryId ?? undefined, this.filterStatus ?? undefined, this.fromDate ?? undefined, this.toDate ?? undefined)
      .subscribe({
        next: (res) => this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) { this.expenses = res.data.items; this.totalCount = res.data.totalCount; }
          this.cdr.detectChanges();
        }),
        error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
      });
  }

  onSearch(v: string): void { if (this.searchTimer) clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => { this.parameters.search = v; this.parameters.page = 1; this.load(); }, 350); }
  onPage(e: any): void { this.parameters.page = Math.floor(e.first / e.rows) + 1; this.parameters.pageSize = e.rows; this.load(); }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({ expenseDate: this.todayIso(), expenseCategoryId: null, amount: 0, paymentMethod: 'Cash', payee: '', referenceNumber: '', description: '' });
    this.form.enable();
    this.dialogVisible = true;
  }

  open(e: ExpenseListItemDto): void {
    this.editingId = e.id; this.dialogError = ''; this.dialogVisible = true; this.form.enable();
    this.svc.getById(e.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const x = res.data;
          this.dialogMode = x.status === 'Draft' ? 'edit' : 'view';
          this.form.patchValue({
            expenseDate: x.expenseDate, expenseCategoryId: x.expenseCategoryId, amount: x.amount,
            paymentMethod: x.paymentMethod, payee: x.payee ?? '', referenceNumber: x.referenceNumber ?? '', description: x.description ?? ''
          });
          if (this.dialogMode === 'view') this.form.disable();
          this.cdr.detectChanges();
        }
      })
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving || this.dialogMode === 'view') return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const payload = {
      expenseDate: v.expenseDate, expenseCategoryId: v.expenseCategoryId, amount: Number(v.amount) || 0,
      paymentMethod: v.paymentMethod, payee: (v.payee as string)?.trim() || null,
      referenceNumber: (v.referenceNumber as string)?.trim() || null, description: (v.description as string)?.trim() || null
    };
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.create(payload).subscribe({ next: done, error: err });
    else this.svc.update(this.editingId!, { id: this.editingId!, ...payload }).subscribe({ next: done, error: err });
  }

  approve(e: ExpenseListItemDto): void { this.rowAction(e, this.svc.approve(e.id)); }
  cancel(e: ExpenseListItemDto): void { this.rowAction(e, this.svc.cancel(e.id)); }
  private rowAction(e: ExpenseListItemDto, obs: any): void {
    if (this.rowActionId) return;
    this.rowActionId = e.id; this.actionError = ''; this.cdr.detectChanges();
    obs.subscribe({
      next: (res: any) => this.zone.run(() => { this.rowActionId = null; if (res.success) this.load(); else this.actionError = res.message || 'Action failed.'; this.cdr.detectChanges(); }),
      error: (er: any) => this.zone.run(() => { this.rowActionId = null; this.actionError = er?.error?.message || 'Action failed.'; this.cdr.detectChanges(); })
    });
  }

  confirmDelete(e: ExpenseListItemDto): void { this.deleting = e; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.delete(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (er) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = er?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }

  statusClass(s: string): string { return s.toLowerCase(); }
}
