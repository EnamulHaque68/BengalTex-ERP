import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SupplierQuotationService } from '../../../services/supplier-quotation.service';
import { SupplierService } from '../../../services/supplier.service';
import { CurrencyService } from '../../../services/currency.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { PurchaseRequisitionService } from '../../../services/purchase-requisition.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { SupplierListItemDto } from '../../../models/supplier.models';
import { CurrencyDto } from '../../../models/master-data.models';
import { RawMaterialListItemDto } from '../../../models/raw-material.models';
import { PurchaseRequisitionDto } from '../../../models/purchase-requisition.models';
import {
  SUPPLIER_QUOTATION_STATUSES, SupplierQuotationListItemDto, QuotationComparisonDto
} from '../../../models/supplier-quotation.models';

@Component({
  selector: 'app-supplier-quotation-list',
  standalone: false,
  templateUrl: './supplier-quotation-list.component.html',
  styleUrl: './supplier-quotation-list.component.scss'
})
export class SupplierQuotationListComponent implements OnInit {
  quotations: SupplierQuotationListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = SUPPLIER_QUOTATION_STATUSES;
  suppliers: SupplierListItemDto[] = [];
  currencies: CurrencyDto[] = [];
  rawMaterials: RawMaterialListItemDto[] = [];
  requisitions: PurchaseRequisitionDto[] = [];

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deletingQuotation: SupplierQuotationListItemDto | null = null;
  deleting = false;
  deleteError = '';

  rowActionId: number | null = null;

  // Comparison
  compareVisible = false;
  comparePrId: number | null = null;
  comparison: QuotationComparisonDto | null = null;
  compareLoading = false;
  compareError = '';

  constructor(
    private svc: SupplierQuotationService,
    private supplierService: SupplierService,
    private currencyService: CurrencyService,
    private rmService: RawMaterialService,
    private prService: PurchaseRequisitionService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadDropdowns();
    this.load();

    // Traceability deep-link: /supplier-quotations?open=<id> opens that quotation's details.
    const open = Number(this.route.snapshot.queryParamMap.get('open'));
    if (open > 0) this.openEdit({ id: open } as SupplierQuotationListItemDto);
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  private buildForm(): void {
    this.form = this.fb.group({
      quotationDate: [this.todayIso(), Validators.required],
      supplierId: [null as number | null, Validators.required],
      purchaseRequisitionId: [null as number | null],
      currencyId: [null as number | null, Validators.required],
      exchangeRate: [1, [Validators.required, Validators.min(0.000001)]],
      validUntil: [null as string | null],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }

  private newLine(rawMaterialId: number | null = null, quantity: number | null = null, unitPrice: number | null = null,
                  leadTimeDays: number | null = null, lineNotes = ''): FormGroup {
    return this.fb.group({
      rawMaterialId: [rawMaterialId, Validators.required],
      quantity: [quantity, [Validators.required, Validators.min(0.0001)]],
      unitPrice: [unitPrice, [Validators.required, Validators.min(0)]],
      leadTimeDays: [leadTimeDays, Validators.min(0)],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  addLine(): void { this.lines.push(this.newLine()); }
  removeLine(i: number): void { this.lines.removeAt(i); }

  rmById(id: number | null | undefined): RawMaterialListItemDto | undefined { return id ? this.rawMaterials.find(r => r.id === id) : undefined; }
  lineUom(l: AbstractControl): string { return this.rmById(l.get('rawMaterialId')?.value)?.unitOfMeasureCode ?? '—'; }
  lineTotal(l: AbstractControl): number { return (Number(l.get('quantity')?.value) || 0) * (Number(l.get('unitPrice')?.value) || 0); }
  grandTotal(): number { return this.lines.controls.reduce((s, l) => s + this.lineTotal(l), 0); }

  onCurrencyChange(currencyId: number): void {
    const c = this.currencies.find(x => x.id === currencyId);
    if (c) this.form.patchValue({ exchangeRate: c.exchangeRateToBase });
  }

  private loadDropdowns(): void {
    this.supplierService.getAll({ page: 1, pageSize: 1000, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.suppliers = res.data.items; this.cdr.detectChanges(); })
    });
    this.currencyService.getAll(false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success) this.currencies = res.data ?? []; this.cdr.detectChanges(); })
    });
    this.rmService.getAll({ page: 1, pageSize: 1000, search: '' }, undefined, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.rawMaterials = res.data.items; this.cdr.detectChanges(); })
    });
    this.prService.getAll({ page: 1, pageSize: 1000, search: '' }).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.requisitions = res.data.items; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters, this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.quotations = res.data.items; this.totalCount = res.data.totalCount; }
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
    const baseCur = this.currencies.find(c => c.isBaseCurrency) ?? this.currencies[0];
    this.form.reset({
      quotationDate: this.todayIso(), supplierId: null, purchaseRequisitionId: null,
      currencyId: baseCur?.id ?? null, exchangeRate: baseCur?.exchangeRateToBase ?? 1, validUntil: null, notes: ''
    });
    this.addLine();
    this.dialogVisible = true;
  }

  openEdit(q: SupplierQuotationListItemDto): void {
    this.editingId = q.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.dialogVisible = true;

    this.svc.getById(q.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const e = res.data;
          this.dialogMode = e.status === 'Draft' ? 'edit' : 'view';
          this.form.patchValue({
            quotationDate: e.quotationDate, supplierId: e.supplierId, purchaseRequisitionId: e.purchaseRequisitionId,
            currencyId: e.currencyId, exchangeRate: e.exchangeRate, validUntil: e.validUntil, notes: e.notes ?? ''
          });
          e.lines.forEach(l => this.lines.push(this.newLine(l.rawMaterialId, l.quantity, l.unitPrice, l.leadTimeDays, l.lineNotes ?? '')));
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
      .filter(l => l.rawMaterialId && Number(l.quantity) > 0)
      .map(l => ({
        rawMaterialId: l.rawMaterialId, quantity: Number(l.quantity), unitPrice: Number(l.unitPrice) || 0,
        leadTimeDays: l.leadTimeDays !== null && l.leadTimeDays !== '' ? Number(l.leadTimeDays) : null,
        lineNotes: (l.lineNotes as string)?.trim() || null
      }));

    if (lines.length === 0) {
      this.dialogSaving = false;
      this.dialogError = 'Add at least one line.';
      this.cdr.detectChanges();
      return;
    }

    const body = {
      quotationDate: v.quotationDate, supplierId: v.supplierId, purchaseRequisitionId: v.purchaseRequisitionId,
      currencyId: v.currencyId, exchangeRate: Number(v.exchangeRate) || 1, validUntil: v.validUntil,
      notes: (v.notes as string)?.trim() || null, lines
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

  rowAction(q: SupplierQuotationListItemDto, kind: 'submit' | 'reject' | 'select'): void {
    if (this.rowActionId) return;
    this.rowActionId = q.id;
    this.actionError = '';
    this.cdr.detectChanges();
    const obs = kind === 'submit' ? this.svc.submit(q.id) : kind === 'reject' ? this.svc.reject(q.id) : this.svc.select(q.id);
    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) this.load(); else this.actionError = res.message || 'Action failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.rowActionId = null; this.actionError = err?.error?.message || 'Action failed.'; this.cdr.detectChanges(); })
    });
  }

  confirmDelete(q: SupplierQuotationListItemDto): void {
    this.deletingQuotation = q;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }
  doDelete(): void {
    if (!this.deletingQuotation || this.deleting) return;
    this.deleting = true;
    this.cdr.detectChanges();
    this.svc.delete(this.deletingQuotation.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) { this.deleteDialogVisible = false; this.deletingQuotation = null; this.load(); }
        else this.deleteError = res.message || 'Delete failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.deleting = false; this.deleteError = err?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }

  // ── Comparison ──
  openCompare(): void {
    this.comparePrId = null;
    this.comparison = null;
    this.compareError = '';
    this.compareVisible = true;
  }

  loadComparison(): void {
    if (!this.comparePrId) { this.comparison = null; return; }
    this.compareLoading = true;
    this.compareError = '';
    this.comparison = null;
    this.cdr.detectChanges();
    this.svc.getComparison(this.comparePrId).subscribe({
      next: (res) => this.zone.run(() => {
        this.compareLoading = false;
        if (res.success && res.data) {
          this.comparison = res.data;
          if (res.data.suppliers.length === 0) this.compareError = 'No submitted quotations for this requisition yet.';
        } else this.compareError = res.message || 'Failed to load comparison.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.compareLoading = false; this.compareError = err?.error?.message || 'Failed to load comparison.'; this.cdr.detectChanges(); })
    });
  }

  cellFor(row: any, supplierQuotationId: number): any {
    return row.cells.find((c: any) => c.supplierQuotationId === supplierQuotationId);
  }

  selectFromCompare(supplierQuotationId: number): void {
    if (this.rowActionId) return;
    this.rowActionId = supplierQuotationId;
    this.cdr.detectChanges();
    this.svc.select(supplierQuotationId).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) { this.loadComparison(); this.load(); }
        else this.compareError = res.message || 'Select failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.rowActionId = null; this.compareError = err?.error?.message || 'Select failed.'; this.cdr.detectChanges(); })
    });
  }

  formatBase(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }
}
