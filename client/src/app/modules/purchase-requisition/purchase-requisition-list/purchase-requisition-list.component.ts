import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PurchaseRequisitionService } from '../../../services/purchase-requisition.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { SupplierService } from '../../../services/supplier.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { CurrencyService } from '../../../services/currency.service';
import { MasterSetupService } from '../../../services/master-setup.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  PurchaseRequisitionDto, PurchaseRequisitionLineDto, PR_STATUSES
} from '../../../models/purchase-requisition.models';
import { RawMaterialListItemDto } from '../../../models/raw-material.models';
import { SupplierListItemDto } from '../../../models/supplier.models';
import { WarehouseDto, CurrencyDto } from '../../../models/master-data.models';
import { DepartmentDto } from '../../../models/master-setup.models';

@Component({
  selector: 'app-purchase-requisition-list',
  standalone: false,
  templateUrl: './purchase-requisition-list.component.html',
  styleUrl: './purchase-requisition-list.component.scss'
})
export class PurchaseRequisitionListComponent implements OnInit {
  prs: PurchaseRequisitionDto[] = [];
  loading = false;
  totalCount = 0;
  actionError = '';
  actionMessage = '';
  rowActionId: number | null = null;

  readonly statuses = PR_STATUSES;
  rawMaterials: RawMaterialListItemDto[] = [];
  suppliers: SupplierListItemDto[] = [];
  warehouses: WarehouseDto[] = [];
  currencies: CurrencyDto[] = [];
  departments: DepartmentDto[] = [];

  filterStatus: string | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Create/edit/view
  dialogVisible = false;
  dialogSaving = false;
  dialogError = '';
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  editing: PurchaseRequisitionDto | null = null;
  form!: FormGroup;
  viewLines: PurchaseRequisitionLineDto[] = [];

  // Decision dialog
  decisionVisible = false;
  decisionTarget: PurchaseRequisitionDto | null = null;
  decisionAction: 'approve' | 'reject' = 'approve';
  decisionNotes = '';
  decisionSaving = false;
  decisionError = '';

  // Cancel
  cancelVisible = false; cancelTarget: PurchaseRequisitionDto | null = null;
  cancelling = false; cancelError = '';

  // Delete
  deleteVisible = false; deleteTarget: PurchaseRequisitionDto | null = null;
  deleting = false; deleteError = '';

  // Convert
  convertVisible = false;
  convertTarget: PurchaseRequisitionDto | null = null;
  convertForm!: FormGroup;
  converting = false;
  convertError = '';

  constructor(
    private service: PurchaseRequisitionService,
    private rmService: RawMaterialService,
    private supplierService: SupplierService,
    private warehouseService: WarehouseService,
    private currencyService: CurrencyService,
    private masterSvc: MasterSetupService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      requisitionDate: [this.todayIso(), Validators.required],
      neededByDate: [this.addDaysIso(14)],
      departmentId: [null],
      departmentText: ['', Validators.maxLength(100)],
      requestedBy: ['', Validators.maxLength(100)],
      purpose: ['', Validators.maxLength(500)],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });

    this.convertForm = this.fb.group({
      supplierId: [null, Validators.required],
      currencyId: [null, Validators.required],
      exchangeRate: [1, [Validators.required, Validators.min(0.000001)]],
      orderDate: [this.todayIso(), Validators.required],
      expectedDeliveryDate: [null],
      deliveryWarehouseId: [null],
      notes: ['', Validators.maxLength(2000)],
      linePrices: this.fb.array([])
    });

    this.loadMasters();
    this.load();

    // Traceability deep-link: /purchase-requisitions?open=<id> opens that requisition's details.
    const open = Number(this.route.snapshot.queryParamMap.get('open'));
    if (open > 0) this.openEdit({ id: open } as PurchaseRequisitionDto);
  }

  private todayIso(): string { return new Date().toISOString().substring(0, 10); }
  private addDaysIso(d: number): string {
    const t = new Date(); t.setDate(t.getDate() + d);
    return t.toISOString().substring(0, 10);
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }
  newLineGroup(rawMaterialId: number | null = null, quantity = 1, estimatedUnitPrice = 0, lineNotes = ''): FormGroup {
    return this.fb.group({
      rawMaterialId: [rawMaterialId, Validators.required],
      quantity: [quantity, [Validators.required, Validators.min(0.0001)]],
      estimatedUnitPrice: [estimatedUnitPrice, [Validators.required, Validators.min(0)]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  get convertLines(): FormArray { return this.convertForm.get('linePrices') as FormArray; }

  formatCurrency(amount: number, code = 'BDT'): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: code, maximumFractionDigits: 2 }).format(amount || 0);
  }

  statusBadgeClass(s: string): string {
    switch (s) {
      case 'Draft': return 'draft';
      case 'Submitted': return 'submitted';
      case 'Approved': return 'approved';
      case 'Rejected': return 'rejected';
      case 'Cancelled': return 'cancelled';
      case 'Converted': return 'converted';
      default: return '';
    }
  }

  private loadMasters(): void {
    this.rmService.getAll({ page: 1, pageSize: 1000, search: '' }).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.rawMaterials = res.data.items; this.cdr.detectChanges(); })
    });
    this.supplierService.getAll({ page: 1, pageSize: 1000, search: '' }).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.suppliers = res.data.items; this.cdr.detectChanges(); })
    });
    this.warehouseService.getAll(undefined, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.warehouses = res.data; this.cdr.detectChanges(); })
    });
    this.currencyService.getAll().subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          this.currencies = res.data;
          const bdt = this.currencies.find(c => c.code === 'BDT');
          if (bdt) this.convertForm.patchValue({ currencyId: bdt.id, exchangeRate: 1 });
        }
        this.cdr.detectChanges();
      })
    });
    this.masterSvc.getDepartments(false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.departments = res.data; this.cdr.detectChanges(); })
    });
  }

  rmName(id: number): string {
    const r = this.rawMaterials.find(x => x.id === id);
    return r ? `${r.name} (${r.code})` : '';
  }
  rmUnit(id: number): string {
    const r = this.rawMaterials.find(x => x.id === id);
    return r?.unitOfMeasureCode ?? '';
  }
  lineTotal(c: any): number {
    return (Number(c.get('quantity')?.value || 0)) * (Number(c.get('estimatedUnitPrice')?.value || 0));
  }
  get formTotal(): number {
    return this.lines.controls.reduce((s, l) => s + this.lineTotal(l), 0);
  }

  load(): void {
    this.loading = true;
    this.service.getAll(this.parameters, this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.prs = res.data.items;
          this.totalCount = res.data.totalCount;
        }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearchChange(value: string): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.parameters.search = value;
      this.parameters.page = 1;
      this.load();
    }, 400);
  }

  onPageChange(event: any): void {
    this.parameters.page = Math.floor(event.first / event.rows) + 1;
    this.parameters.pageSize = event.rows;
    this.load();
  }

  // ── Create / Edit / View ─────────────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editing = null;
    this.dialogError = '';
    this.form.reset({
      requisitionDate: this.todayIso(),
      neededByDate: this.addDaysIso(14),
      departmentId: null, departmentText: '',
      requestedBy: '', purpose: '', notes: ''
    });
    this.lines.clear();
    this.lines.push(this.newLineGroup());
    this.form.enable();
    this.dialogVisible = true;
  }

  openEdit(p: PurchaseRequisitionDto): void {
    this.dialogMode = p.status === 'Draft' ? 'edit' : 'view';
    this.editing = p;
    this.dialogError = '';
    this.service.getById(p.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const full = res.data;
          this.form.patchValue({
            requisitionDate: full.requisitionDate,
            neededByDate: full.neededByDate,
            departmentId: full.departmentId,
            departmentText: full.departmentText ?? '',
            requestedBy: full.requestedBy ?? '',
            purpose: full.purpose ?? '',
            notes: full.notes ?? ''
          });
          this.lines.clear();
          full.lines.forEach(l => this.lines.push(this.newLineGroup(
            l.rawMaterialId, l.quantity, l.estimatedUnitPrice, l.lineNotes ?? ''
          )));
          this.viewLines = full.lines;
          if (this.dialogMode === 'view') this.form.disable(); else this.form.enable();
          this.dialogVisible = true;
        }
        this.cdr.detectChanges();
      })
    });
  }

  addLine(): void { this.lines.push(this.newLineGroup()); }
  removeLine(i: number): void { this.lines.removeAt(i); }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const linesPayload = (v.lines as any[]).map(l => ({
      rawMaterialId: Number(l.rawMaterialId),
      quantity: Number(l.quantity),
      estimatedUnitPrice: Number(l.estimatedUnitPrice),
      lineNotes: (l.lineNotes as string)?.trim() || null
    }));
    const body = {
      requisitionDate: v.requisitionDate,
      neededByDate: v.neededByDate || null,
      departmentId: v.departmentId ?? null,
      departmentText: (v.departmentText as string)?.trim() || null,
      requestedBy: (v.requestedBy as string)?.trim() || null,
      purpose: (v.purpose as string)?.trim() || null,
      notes: (v.notes as string)?.trim() || null,
      lines: linesPayload
    };
    const obs: any = this.dialogMode === 'create'
      ? this.service.create(body)
      : this.service.update(this.editing!.id, body);
    obs.subscribe({
      next: (res: any) => this.zone.run(() => {
        this.dialogSaving = false;
        if (res.success) { this.dialogVisible = false; this.actionMessage = res.message || 'Saved.'; this.load(); }
        else this.dialogError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err: any) => this.zone.run(() => {
        this.dialogSaving = false;
        this.dialogError = err?.error?.message || 'Save failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Submit ──────────────────────────────────────────────────────────────

  submit(p: PurchaseRequisitionDto): void {
    if (this.rowActionId) return;
    if (!confirm(`Submit ${p.code} for approval?`)) return;
    this.rowActionId = p.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.service.submit(p.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) { this.actionMessage = res.message || 'Submitted.'; this.load(); }
        else this.actionError = res.message || 'Submit failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.rowActionId = null;
        this.actionError = err?.error?.message || 'Submit failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Decision (Approve / Reject) ─────────────────────────────────────────

  openDecision(p: PurchaseRequisitionDto, action: 'approve' | 'reject'): void {
    this.decisionTarget = p;
    this.decisionAction = action;
    this.decisionNotes = '';
    this.decisionError = '';
    this.decisionVisible = true;
  }

  doDecision(): void {
    if (!this.decisionTarget || this.decisionSaving) return;
    this.decisionSaving = true;
    this.decisionError = '';
    this.cdr.detectChanges();
    const obs = this.decisionAction === 'approve'
      ? this.service.approve(this.decisionTarget.id, this.decisionNotes?.trim() || null)
      : this.service.reject(this.decisionTarget.id, this.decisionNotes?.trim() || null);
    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.decisionSaving = false;
        if (res.success) { this.decisionVisible = false; this.actionMessage = res.message || 'Done.'; this.load(); }
        else this.decisionError = res.message || 'Action failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.decisionSaving = false;
        this.decisionError = err?.error?.message || 'Action failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Cancel ──────────────────────────────────────────────────────────────

  openCancel(p: PurchaseRequisitionDto): void { this.cancelTarget = p; this.cancelError = ''; this.cancelVisible = true; }
  doCancel(): void {
    if (!this.cancelTarget || this.cancelling) return;
    this.cancelling = true; this.cancelError = ''; this.cdr.detectChanges();
    this.service.cancel(this.cancelTarget.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.cancelling = false;
        if (res.success) { this.cancelVisible = false; this.actionMessage = res.message || 'Cancelled.'; this.load(); }
        else this.cancelError = res.message || 'Cancel failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.cancelling = false;
        this.cancelError = err?.error?.message || 'Cancel failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Delete ──────────────────────────────────────────────────────────────

  openDelete(p: PurchaseRequisitionDto): void { this.deleteTarget = p; this.deleteError = ''; this.deleteVisible = true; }
  doDelete(): void {
    if (!this.deleteTarget || this.deleting) return;
    this.deleting = true; this.deleteError = ''; this.cdr.detectChanges();
    this.service.delete(this.deleteTarget.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) { this.deleteVisible = false; this.actionMessage = res.message || 'Deleted.'; this.load(); }
        else this.deleteError = res.message || 'Delete failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.deleting = false;
        this.deleteError = err?.error?.message || 'Delete failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Convert to PO ───────────────────────────────────────────────────────

  openConvert(p: PurchaseRequisitionDto): void {
    this.convertTarget = p;
    this.convertError = '';
    this.service.getById(p.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const full = res.data;
          this.convertTarget = full;
          const bdt = this.currencies.find(c => c.code === 'BDT');
          this.convertForm.patchValue({
            supplierId: null,
            currencyId: bdt?.id ?? null,
            exchangeRate: 1,
            orderDate: this.todayIso(),
            expectedDeliveryDate: full.neededByDate,
            deliveryWarehouseId: null,
            notes: `Converted from PR ${full.code}`
          });
          this.convertLines.clear();
          full.lines.forEach(l => this.convertLines.push(this.fb.group({
            purchaseRequisitionLineId: [l.id],
            rawMaterialId: [l.rawMaterialId],
            rawMaterialName: [l.rawMaterialName],
            rawMaterialCode: [l.rawMaterialCode],
            unit: [l.rawMaterialUnit ?? ''],
            quantity: [l.quantity],
            unitPrice: [l.estimatedUnitPrice, [Validators.required, Validators.min(0)]]
          })));
          this.convertVisible = true;
        }
        this.cdr.detectChanges();
      })
    });
  }

  onConvertCurrencyChange(ev: any): void {
    const c = this.currencies.find(x => x.id === ev?.value);
    if (c) this.convertForm.patchValue({ exchangeRate: c.exchangeRateToBase });
  }

  doConvert(): void {
    if (!this.convertTarget || this.convertForm.invalid || this.converting) return;
    this.converting = true; this.convertError = ''; this.cdr.detectChanges();
    const v = this.convertForm.getRawValue();
    const linePrices = (v.linePrices as any[]).map(l => ({
      purchaseRequisitionLineId: Number(l.purchaseRequisitionLineId),
      unitPrice: Number(l.unitPrice)
    }));
    this.service.convert(this.convertTarget.id, {
      supplierId: Number(v.supplierId),
      currencyId: Number(v.currencyId),
      exchangeRate: Number(v.exchangeRate),
      orderDate: v.orderDate,
      expectedDeliveryDate: v.expectedDeliveryDate || null,
      deliveryWarehouseId: v.deliveryWarehouseId ?? null,
      notes: (v.notes as string)?.trim() || null,
      linePrices
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.converting = false;
        if (res.success) { this.convertVisible = false; this.actionMessage = res.message || 'Converted.'; this.load(); }
        else this.convertError = res.message || 'Convert failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.converting = false;
        this.convertError = err?.error?.message || 'Convert failed.';
        this.cdr.detectChanges();
      })
    });
  }
}
