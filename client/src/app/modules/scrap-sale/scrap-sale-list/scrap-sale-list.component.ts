import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ScrapSaleService } from '../../../services/scrap-sale.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { SCRAP_SALE_STATUSES, SCRAP_PAYMENT_METHODS, ScrapSaleListItemDto } from '../../../models/scrap-sale.models';

@Component({
  selector: 'app-scrap-sale-list',
  standalone: false,
  templateUrl: './scrap-sale-list.component.html',
  styleUrl: './scrap-sale-list.component.scss'
})
export class ScrapSaleListComponent implements OnInit {
  sales: ScrapSaleListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = SCRAP_SALE_STATUSES;
  readonly paymentMethods = SCRAP_PAYMENT_METHODS;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deletingSale: ScrapSaleListItemDto | null = null;
  deleting = false;
  deleteError = '';

  rowActionId: number | null = null;

  constructor(
    private svc: ScrapSaleService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  private buildForm(): void {
    this.form = this.fb.group({
      saleDate: [this.todayIso(), Validators.required],
      buyerName: ['', Validators.maxLength(200)],
      paymentMethod: ['Cash', Validators.required],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }

  private newLine(description = '', quantity: number | null = null, unit: string | null = null, unitPrice: number | null = null): FormGroup {
    return this.fb.group({
      description: [description, [Validators.required, Validators.maxLength(300)]],
      quantity: [quantity, [Validators.required, Validators.min(0.0001)]],
      unit: [unit, Validators.maxLength(20)],
      unitPrice: [unitPrice, [Validators.required, Validators.min(0)]]
    });
  }

  addLine(): void { this.lines.push(this.newLine()); }
  removeLine(i: number): void { this.lines.removeAt(i); }

  lineTotal(line: AbstractControl): number {
    return (Number(line.get('quantity')?.value) || 0) * (Number(line.get('unitPrice')?.value) || 0);
  }
  grandTotal(): number {
    return this.lines.controls.reduce((s, l) => s + this.lineTotal(l), 0);
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters, this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.sales = res.data.items; this.totalCount = res.data.totalCount; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearchChange(value: string): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.parameters.search = value; this.parameters.page = 1; this.load(); }, 400);
  }
  onPageChange(event: any): void {
    this.parameters.page = Math.floor(event.first / event.rows) + 1;
    this.parameters.pageSize = event.rows;
    this.load();
  }

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.enable();
    this.lines.clear();
    this.form.reset({ saleDate: this.todayIso(), buyerName: '', paymentMethod: 'Cash', notes: '' });
    this.addLine();
    this.dialogVisible = true;
  }

  openEdit(sale: ScrapSaleListItemDto): void {
    this.editingId = sale.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.dialogVisible = true;

    this.svc.getById(sale.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const e = res.data;
          this.dialogMode = e.status === 'Draft' ? 'edit' : 'view';
          this.form.patchValue({ saleDate: e.saleDate, buyerName: e.buyerName ?? '', paymentMethod: e.paymentMethod, notes: e.notes ?? '' });
          e.lines.forEach(l => this.lines.push(this.newLine(l.description, l.quantity, l.unit, l.unitPrice)));
          if (this.dialogMode === 'view') this.form.disable();
          this.cdr.detectChanges();
        }
      })
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving || this.dialogMode === 'view') return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();
    const lines = (v.lines as any[])
      .filter(l => l.description && Number(l.quantity) > 0)
      .map(l => ({
        description: (l.description as string).trim(),
        quantity: Number(l.quantity),
        unit: (l.unit as string)?.trim() || null,
        unitPrice: Number(l.unitPrice) || 0
      }));

    if (lines.length === 0) {
      this.dialogSaving = false;
      this.dialogError = 'Add at least one line with a description and quantity.';
      this.cdr.detectChanges();
      return;
    }

    const body = {
      saleDate: v.saleDate,
      buyerName: (v.buyerName as string)?.trim() || null,
      paymentMethod: v.paymentMethod,
      notes: (v.notes as string)?.trim() || null,
      lines
    };
    const obs = this.dialogMode === 'create' ? this.svc.create(body) : this.svc.update(this.editingId!, body);
    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.dialogSaving = false;
        if (res.success) { this.dialogVisible = false; this.load(); }
        else this.dialogError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = err?.error?.message || 'Save failed.'; this.cdr.detectChanges(); })
    });
  }

  post(sale: ScrapSaleListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = sale.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.svc.post(sale.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) this.load(); else this.actionError = res.message || 'Post failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.rowActionId = null; this.actionError = err?.error?.message || 'Post failed.'; this.cdr.detectChanges(); })
    });
  }

  confirmDelete(sale: ScrapSaleListItemDto): void {
    this.deletingSale = sale;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }
  doDelete(): void {
    if (!this.deletingSale || this.deleting) return;
    this.deleting = true;
    this.cdr.detectChanges();
    this.svc.delete(this.deletingSale.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) { this.deleteDialogVisible = false; this.deletingSale = null; this.load(); }
        else this.deleteError = res.message || 'Delete failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.deleting = false; this.deleteError = err?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }
}
