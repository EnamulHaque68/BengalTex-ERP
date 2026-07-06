import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LandedCostService } from '../../../services/landed-cost.service';
import { GoodsReceiptService } from '../../../services/goods-receipt.service';
import { SupplierService } from '../../../services/supplier.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { GoodsReceiptListItemDto } from '../../../models/goods-receipt.models';
import { SupplierListItemDto } from '../../../models/supplier.models';
import {
  LANDED_COST_STATUSES, LANDED_COST_ALLOCATION_BASES, LANDED_COST_CHARGE_TYPES, LANDED_COST_PAYMENT_METHODS,
  LandedCostVoucherListItemDto, LandedCostAllocationLineDto
} from '../../../models/landed-cost.models';

@Component({
  selector: 'app-landed-cost-list',
  standalone: false,
  templateUrl: './landed-cost-list.component.html',
  styleUrl: './landed-cost-list.component.scss'
})
export class LandedCostListComponent implements OnInit {
  vouchers: LandedCostVoucherListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = LANDED_COST_STATUSES;
  readonly bases = LANDED_COST_ALLOCATION_BASES;
  readonly chargeTypes = LANDED_COST_CHARGE_TYPES;
  readonly paymentMethods = LANDED_COST_PAYMENT_METHODS;

  postedGrns: GoodsReceiptListItemDto[] = [];
  suppliers: SupplierListItemDto[] = [];   // Phase A2 — on-credit agent picker

  // Phase A2 — settle dialog
  settleVisible = false;
  settling = false;
  settleError = '';
  settleTarget: LandedCostVoucherListItemDto | null = null;
  settleDate = '';
  settleMethod = 'BankTransfer';

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;
  allocationPreview: LandedCostAllocationLineDto[] = [];   // shown in view mode

  deleteDialogVisible = false;
  deletingVoucher: LandedCostVoucherListItemDto | null = null;
  deleting = false;
  deleteError = '';

  rowActionId: number | null = null;

  constructor(
    private svc: LandedCostService,
    private grnService: GoodsReceiptService,
    private supplierService: SupplierService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  // ── Phase A2 — settle an on-credit voucher ──
  openSettle(v: LandedCostVoucherListItemDto): void {
    this.settleTarget = v;
    this.settleDate = this.todayIso();
    this.settleMethod = 'BankTransfer';
    this.settleError = '';
    this.settleVisible = true;
  }
  confirmSettle(): void {
    if (!this.settleTarget || this.settling) return;
    this.settling = true;
    this.settleError = '';
    this.svc.settle(this.settleTarget.id, this.settleDate, this.settleMethod).subscribe({
      next: (res) => this.zone.run(() => {
        this.settling = false;
        if (res.success) { this.settleVisible = false; this.load(); }
        else this.settleError = res.message || 'Settle failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.settling = false;
        this.settleError = err?.error?.message || 'Settle failed.';
        this.cdr.detectChanges();
      })
    });
  }

  ngOnInit(): void {
    this.buildForm();
    this.loadGrns();
    this.supplierService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.suppliers = res.data.items; this.cdr.detectChanges(); })
    });
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  private buildForm(): void {
    this.form = this.fb.group({
      voucherDate: [this.todayIso(), Validators.required],
      goodsReceiptNoteId: [null as number | null, Validators.required],
      allocationBasis: ['ByValue', Validators.required],
      paymentMethod: ['BankTransfer', Validators.required],
      isOnCredit: [false],                     // Phase A2
      supplierId: [null as number | null],     // Phase A2 — agent/supplier when on credit
      notes: ['', Validators.maxLength(2000)],
      charges: this.fb.array([])
    });
  }

  get charges(): FormArray { return this.form.get('charges') as FormArray; }

  private newCharge(chargeType = 'Freight', amount: number | null = null, notes = ''): FormGroup {
    return this.fb.group({
      chargeType: [chargeType, Validators.required],
      amount: [amount, [Validators.required, Validators.min(0.0001)]],
      notes: [notes, Validators.maxLength(500)]
    });
  }

  addCharge(): void { this.charges.push(this.newCharge()); }
  removeCharge(i: number): void { this.charges.removeAt(i); }

  chargeAmount(c: AbstractControl): number { return Number(c.get('amount')?.value) || 0; }
  totalCharges(): number { return this.charges.controls.reduce((s, c) => s + this.chargeAmount(c), 0); }

  private loadGrns(): void {
    this.grnService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, 'Posted').subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.postedGrns = res.data.items;
        this.cdr.detectChanges();
      })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters, this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.vouchers = res.data.items; this.totalCount = res.data.totalCount; }
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
    this.allocationPreview = [];
    this.form.enable();
    this.charges.clear();
    this.form.reset({ voucherDate: this.todayIso(), goodsReceiptNoteId: null, allocationBasis: 'ByValue', paymentMethod: 'BankTransfer', isOnCredit: false, supplierId: null, notes: '' });
    this.addCharge();
    this.dialogVisible = true;
  }

  openEdit(v: LandedCostVoucherListItemDto): void {
    this.editingId = v.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.allocationPreview = [];
    this.form.enable();
    this.charges.clear();
    this.dialogVisible = true;

    this.svc.getById(v.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const e = res.data;
          this.dialogMode = e.status === 'Draft' ? 'edit' : 'view';
          this.allocationPreview = e.allocation;
          this.form.patchValue({
            voucherDate: e.voucherDate, goodsReceiptNoteId: e.goodsReceiptNoteId,
            allocationBasis: e.allocationBasis, paymentMethod: e.paymentMethod, notes: e.notes ?? '',
            isOnCredit: e.isOnCredit, supplierId: e.supplierId
          });
          e.charges.forEach(c => this.charges.push(this.newCharge(c.chargeType, c.amount, c.notes ?? '')));
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
    const charges = (v.charges as any[])
      .filter(c => c.chargeType && Number(c.amount) > 0)
      .map(c => ({ chargeType: c.chargeType, amount: Number(c.amount), notes: (c.notes as string)?.trim() || null }));

    if (charges.length === 0) {
      this.dialogSaving = false;
      this.dialogError = 'Add at least one charge with an amount.';
      this.cdr.detectChanges();
      return;
    }

    if (v.isOnCredit && !v.supplierId) {
      this.dialogSaving = false;
      this.dialogError = 'Select the agent/supplier the charges are owed to when booking on credit.';
      this.cdr.detectChanges();
      return;
    }

    const body = {
      voucherDate: v.voucherDate, goodsReceiptNoteId: v.goodsReceiptNoteId,
      allocationBasis: v.allocationBasis, paymentMethod: v.paymentMethod,
      notes: (v.notes as string)?.trim() || null, charges,
      isOnCredit: !!v.isOnCredit, supplierId: v.isOnCredit ? v.supplierId : null   // Phase A2
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

  post(v: LandedCostVoucherListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = v.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.svc.post(v.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) this.load(); else this.actionError = res.message || 'Post failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.rowActionId = null; this.actionError = err?.error?.message || 'Post failed.'; this.cdr.detectChanges(); })
    });
  }

  confirmDelete(v: LandedCostVoucherListItemDto): void {
    this.deletingVoucher = v;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }
  doDelete(): void {
    if (!this.deletingVoucher || this.deleting) return;
    this.deleting = true;
    this.cdr.detectChanges();
    this.svc.delete(this.deletingVoucher.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) { this.deleteDialogVisible = false; this.deletingVoucher = null; this.load(); }
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
