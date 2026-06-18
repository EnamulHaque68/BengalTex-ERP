import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { OpeningStockService } from '../../../services/opening-stock.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { ProductService } from '../../../services/product.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { OPENING_STOCK_STATUSES, OpeningStockListItemDto } from '../../../models/opening-stock.models';
import { WarehouseDto } from '../../../models/master-data.models';

interface ItemOption { key: string; label: string; itemType: string; itemId: number; uom: string; }

@Component({
  selector: 'app-opening-stock-list',
  standalone: false,
  templateUrl: './opening-stock-list.component.html',
  styleUrl: './opening-stock-list.component.scss'
})
export class OpeningStockListComponent implements OnInit {
  entries: OpeningStockListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = OPENING_STOCK_STATUSES;
  warehouses: WarehouseDto[] = [];
  itemOptions: ItemOption[] = [];

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deletingEntry: OpeningStockListItemDto | null = null;
  deleting = false;
  deleteError = '';

  rowActionId: number | null = null;

  constructor(
    private svc: OpeningStockService,
    private warehouseService: WarehouseService,
    private rmService: RawMaterialService,
    private productService: ProductService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadDropdowns();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  private buildForm(): void {
    this.form = this.fb.group({
      entryDate: [this.todayIso(), Validators.required],
      warehouseId: [null as number | null, Validators.required],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }

  private newLine(itemKey: string | null = null, quantity: number | null = null, unitCost: number | null = null, lineNotes = ''): FormGroup {
    return this.fb.group({
      itemKey: [itemKey, Validators.required],
      quantity: [quantity, [Validators.required, Validators.min(0.0001)]],
      unitCost: [unitCost, [Validators.required, Validators.min(0)]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  addLine(): void { this.lines.push(this.newLine()); }
  removeLine(i: number): void { this.lines.removeAt(i); }

  optionByKey(key: string | null | undefined): ItemOption | undefined {
    return key ? this.itemOptions.find(o => o.key === key) : undefined;
  }
  lineUom(line: AbstractControl): string {
    return this.optionByKey(line.get('itemKey')?.value)?.uom ?? '—';
  }
  lineValue(line: AbstractControl): number {
    return (Number(line.get('quantity')?.value) || 0) * (Number(line.get('unitCost')?.value) || 0);
  }
  totalValue(): number {
    return this.lines.controls.reduce((s, l) => s + this.lineValue(l), 0);
  }

  private loadDropdowns(): void {
    this.warehouseService.getAll(undefined, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success) this.warehouses = res.data ?? []; this.cdr.detectChanges(); })
    });
    const params = { page: 1, pageSize: 1000, search: '' };
    this.rmService.getAll(params, undefined, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          for (const r of res.data.items)
            this.itemOptions.push({ key: `RawMaterial:${r.id}`, label: `RM · ${r.code} — ${r.name}`, itemType: 'RawMaterial', itemId: r.id, uom: r.unitOfMeasureCode });
        }
        this.cdr.detectChanges();
      })
    });
    this.productService.getAll(params, undefined, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          for (const p of res.data.items)
            this.itemOptions.push({ key: `Product:${p.id}`, label: `FG · ${p.code} — ${p.name}`, itemType: 'Product', itemId: p.id, uom: p.unitOfMeasureCode });
        }
        this.cdr.detectChanges();
      })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters, this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.entries = res.data.items; this.totalCount = res.data.totalCount; }
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
    this.form.reset({ entryDate: this.todayIso(), warehouseId: this.warehouses[0]?.id ?? null, notes: '' });
    this.addLine();
    this.dialogVisible = true;
  }

  openEdit(entry: OpeningStockListItemDto): void {
    this.editingId = entry.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.dialogVisible = true;

    this.svc.getById(entry.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const e = res.data;
          this.dialogMode = e.status === 'Draft' ? 'edit' : 'view';
          this.form.patchValue({ entryDate: e.entryDate, warehouseId: e.warehouseId, notes: e.notes ?? '' });
          e.lines.forEach(l => this.lines.push(this.newLine(`${l.itemType}:${l.rawMaterialId ?? l.productId}`, l.quantity, l.unitCost, l.lineNotes ?? '')));
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
      .filter(l => l.itemKey && Number(l.quantity) > 0)
      .map(l => {
        const opt = this.optionByKey(l.itemKey)!;
        return { itemType: opt.itemType, itemId: opt.itemId, quantity: Number(l.quantity), unitCost: Number(l.unitCost) || 0, lineNotes: (l.lineNotes as string)?.trim() || null };
      });

    if (lines.length === 0) {
      this.dialogSaving = false;
      this.dialogError = 'Add at least one line with a quantity.';
      this.cdr.detectChanges();
      return;
    }

    const body = { entryDate: v.entryDate, warehouseId: v.warehouseId, notes: (v.notes as string)?.trim() || null, lines };
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

  post(entry: OpeningStockListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = entry.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.svc.post(entry.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) this.load(); else this.actionError = res.message || 'Post failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.rowActionId = null; this.actionError = err?.error?.message || 'Post failed.'; this.cdr.detectChanges(); })
    });
  }

  confirmDelete(entry: OpeningStockListItemDto): void {
    this.deletingEntry = entry;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }
  doDelete(): void {
    if (!this.deletingEntry || this.deleting) return;
    this.deleting = true;
    this.cdr.detectChanges();
    this.svc.delete(this.deletingEntry.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) { this.deleteDialogVisible = false; this.deletingEntry = null; this.load(); }
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
