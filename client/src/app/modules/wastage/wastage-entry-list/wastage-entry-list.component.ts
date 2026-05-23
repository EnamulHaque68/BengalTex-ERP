import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { WastageService } from '../../../services/wastage.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { ProductionOrderService } from '../../../services/production-order.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { WastageEntryListItemDto, WastageReasonDto } from '../../../models/wastage.models';

@Component({
  selector: 'app-wastage-entry-list',
  standalone: false,
  templateUrl: './wastage-entry-list.component.html',
  styleUrl: './wastage-entry-list.component.scss'
})
export class WastageEntryListComponent implements OnInit {
  entries: WastageEntryListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterReasonId: number | null = null;
  fromDate: string | null = null;
  toDate: string | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  reasons: WastageReasonDto[] = [];
  rawMaterials: any[] = [];
  productionOrders: any[] = [];

  canCreate = false;
  canEdit = false;
  canDelete = false;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deleting: WastageEntryListItemDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(
    private svc: WastageService,
    private rmService: RawMaterialService,
    private poService: ProductionOrderService,
    private auth: AuthService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canCreate = this.auth.hasPermission('Wastage.Create');
    this.canEdit = this.auth.hasPermission('Wastage.Edit');
    this.canDelete = this.auth.hasPermission('Wastage.Delete');
    this.form = this.fb.group({
      wastageDate: [this.todayIso(), Validators.required],
      productionOrderId: [null as number | null],
      rawMaterialId: [null as number | null, Validators.required],
      wastageReasonId: [null as number | null, Validators.required],
      quantity: [1, [Validators.required, Validators.min(0.0001)]],
      department: ['', Validators.maxLength(150)],
      notes: ['', Validators.maxLength(1000)]
    });
    this.loadDropdowns();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  private loadDropdowns(): void {
    this.svc.getReasons(false).subscribe({ next: (res) => this.zone.run(() => { if (res.success && res.data) this.reasons = res.data; this.cdr.detectChanges(); }) });
    this.rmService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.rawMaterials = res.data.items; this.cdr.detectChanges(); })
    });
    this.poService.getAll({ page: 1, pageSize: 200, search: '' }).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.productionOrders = res.data.items; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters, undefined, this.filterReasonId ?? undefined, this.fromDate ?? undefined, this.toDate ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.entries = res.data.items; this.totalCount = res.data.totalCount; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearch(v: string): void { if (this.searchTimer) clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => { this.parameters.search = v; this.parameters.page = 1; this.load(); }, 350); }
  onPage(e: any): void { this.parameters.page = Math.floor(e.first / e.rows) + 1; this.parameters.pageSize = e.rows; this.load(); }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({ wastageDate: this.todayIso(), productionOrderId: null, rawMaterialId: null, wastageReasonId: null, quantity: 1, department: '', notes: '' });
    this.dialogVisible = true;
  }
  open(e: WastageEntryListItemDto): void {
    this.dialogMode = 'edit'; this.editingId = e.id; this.dialogError = ''; this.dialogVisible = true;
    this.svc.getById(e.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const x = res.data;
          this.form.reset({
            wastageDate: x.wastageDate, productionOrderId: x.productionOrderId, rawMaterialId: x.rawMaterialId,
            wastageReasonId: x.wastageReasonId, quantity: x.quantity, department: x.department ?? '', notes: x.notes ?? ''
          });
          this.cdr.detectChanges();
        }
      })
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = {
      wastageDate: v.wastageDate, productionOrderId: v.productionOrderId ?? null, rawMaterialId: v.rawMaterialId,
      wastageReasonId: v.wastageReasonId, quantity: Number(v.quantity) || 0,
      department: (v.department as string)?.trim() || null, notes: (v.notes as string)?.trim() || null
    };
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.create(base).subscribe({ next: done, error: err });
    else this.svc.update(this.editingId!, { id: this.editingId!, ...base }).subscribe({ next: done, error: err });
  }

  confirmDelete(e: WastageEntryListItemDto): void { this.deleting = e; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.delete(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }
}
