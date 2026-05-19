import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { StockTransferService } from '../../../services/stock-transfer.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { ProductService } from '../../../services/product.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  STOCK_TRANSFER_STATUSES,
  StockTransferListItemDto
} from '../../../models/stock-transfer.models';
import { WarehouseDto } from '../../../models/master-data.models';

/// Combined RM + Product picker option. itemKey is unique across both types — used as the
/// dropdown's `optionValue` so we can encode the polymorphic choice in a single field.
interface ItemPickerOption {
  itemKey: string;                   // "RM:123" or "P:456"
  itemType: 'RawMaterial' | 'Product';
  itemId: number;
  code: string;
  name: string;
  uomCode: string;
  displayLabel: string;              // "[RM] CODE — Name (uom)"
}

@Component({
  selector: 'app-stock-transfer-list',
  standalone: false,
  templateUrl: './stock-transfer-list.component.html',
  styleUrl: './stock-transfer-list.component.scss'
})
export class StockTransferListComponent implements OnInit {

  transfers: StockTransferListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterSourceWarehouseId: number | null = null;
  filterDestinationWarehouseId: number | null = null;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = STOCK_TRANSFER_STATUSES;
  warehouses: WarehouseDto[] = [];
  itemOptions: ItemPickerOption[] = [];

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deletingTxfr: StockTransferListItemDto | null = null;
  deleting = false;
  deleteError = '';

  rowActionId: number | null = null;

  constructor(
    private txfrService: StockTransferService,
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

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private buildForm(): void {
    this.form = this.fb.group({
      sourceWarehouseId: [null as number | null, Validators.required],
      destinationWarehouseId: [null as number | null, Validators.required],
      transferDate: [this.todayIso(), Validators.required],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  private loadDropdowns(): void {
    this.warehouseService.getAll(undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.warehouses = res.data;
          this.cdr.detectChanges();
        });
      }
    });
    this.rmService.getAll({ page: 1, pageSize: 1000, search: '' }, undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const rmOpts: ItemPickerOption[] = res.data.items.map(rm => ({
              itemKey: `RM:${rm.id}`,
              itemType: 'RawMaterial' as const,
              itemId: rm.id,
              code: rm.code,
              name: rm.name,
              uomCode: rm.unitOfMeasureCode,
              displayLabel: `[RM] ${rm.code} — ${rm.name} (${rm.unitOfMeasureCode})`
            }));
            this.itemOptions = [...this.itemOptions.filter(o => o.itemType !== 'RawMaterial'), ...rmOpts]
              .sort((a, b) => a.displayLabel.localeCompare(b.displayLabel));
          }
          this.cdr.detectChanges();
        });
      }
    });
    this.productService.getAll({ page: 1, pageSize: 1000, search: '' }, undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const prodOpts: ItemPickerOption[] = res.data.items.map(p => ({
              itemKey: `P:${p.id}`,
              itemType: 'Product' as const,
              itemId: p.id,
              code: p.code,
              name: p.name,
              uomCode: p.unitOfMeasureCode,
              displayLabel: `[Product] ${p.code} — ${p.name} (${p.unitOfMeasureCode})`
            }));
            this.itemOptions = [...this.itemOptions.filter(o => o.itemType !== 'Product'), ...prodOpts]
              .sort((a, b) => a.displayLabel.localeCompare(b.displayLabel));
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.txfrService.getAll(
      this.parameters,
      this.filterSourceWarehouseId ?? undefined,
      this.filterDestinationWarehouseId ?? undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.transfers = res.data.items;
            this.totalCount = res.data.totalCount;
          }
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); });
      }
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

  private newLine(itemKey: string | null, uomCode: string, quantity: number, lineNotes = ''): FormGroup {
    return this.fb.group({
      itemKey: [itemKey, Validators.required],
      uomCode: [uomCode],
      quantity: [quantity, [Validators.required, Validators.min(0.0001)]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  addLine(): void {
    this.lines.push(this.newLine(null, '', 0));
  }

  removeLine(index: number): void {
    this.lines.removeAt(index);
  }

  onLineItemChange(index: number): void {
    const lineCtrl = this.lines.at(index);
    const itemKey = lineCtrl.get('itemKey')?.value;
    const opt = this.itemOptions.find(o => o.itemKey === itemKey);
    lineCtrl.patchValue({ uomCode: opt?.uomCode ?? '' });
  }

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      sourceWarehouseId: null,
      destinationWarehouseId: null,
      transferDate: this.todayIso(),
      notes: ''
    });
    this.addLine();
    this.dialogVisible = true;
  }

  openEdit(t: StockTransferListItemDto): void {
    this.editingId = t.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.dialogVisible = true;

    this.txfrService.getById(t.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const s = res.data;
            this.dialogMode = s.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              sourceWarehouseId: s.sourceWarehouseId,
              destinationWarehouseId: s.destinationWarehouseId,
              transferDate: s.transferDate,
              notes: s.notes ?? ''
            });
            for (const l of s.lines) {
              const itemKey = l.itemType === 'RawMaterial'
                ? `RM:${l.rawMaterialId}`
                : `P:${l.productId}`;
              this.lines.push(this.newLine(itemKey, l.unitOfMeasureCode, l.quantity, l.lineNotes ?? ''));
            }
            if (this.dialogMode === 'view') this.form.disable();
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving || this.dialogMode === 'view') return;

    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();
    const lines = (v.lines as any[]).map(l => {
      const opt = this.itemOptions.find(o => o.itemKey === l.itemKey);
      return {
        rawMaterialId: opt?.itemType === 'RawMaterial' ? opt.itemId : null,
        productId: opt?.itemType === 'Product' ? opt.itemId : null,
        quantity: Number(l.quantity) || 0,
        lineNotes: (l.lineNotes as string)?.trim() || null
      };
    });

    if (this.dialogMode === 'create') {
      this.txfrService.create({
        sourceWarehouseId: v.sourceWarehouseId,
        destinationWarehouseId: v.destinationWarehouseId,
        transferDate: v.transferDate,
        notes: (v.notes as string)?.trim() || null,
        lines
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.txfrService.update(this.editingId, {
        sourceWarehouseId: v.sourceWarehouseId,
        destinationWarehouseId: v.destinationWarehouseId,
        transferDate: v.transferDate,
        notes: (v.notes as string)?.trim() || null,
        lines
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    }
  }

  private handleSave(res: { success: boolean; message?: string | null }): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      if (res.success) {
        this.dialogVisible = false;
        this.load();
      } else {
        this.dialogError = res.message || 'Save failed.';
      }
      this.cdr.detectChanges();
    });
  }

  private handleError(err: any): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      this.dialogError = err?.error?.message || 'Save failed.';
      this.cdr.detectChanges();
    });
  }

  post(t: StockTransferListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = t.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.txfrService.post(t.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.rowActionId = null;
          if (res.success) {
            this.actionError = '';
            this.load();
          } else {
            this.actionError = res.message || 'Post failed.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.rowActionId = null;
          this.actionError = err?.error?.message || 'Post failed.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  canDelete(t: StockTransferListItemDto): boolean {
    return t.status === 'Draft';
  }

  confirmDelete(t: StockTransferListItemDto): void {
    this.deletingTxfr = t;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingTxfr || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.txfrService.delete(this.deletingTxfr.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingTxfr = null;
            this.load();
          } else {
            this.deleteError = res.message || 'Delete failed.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.deleting = false;
          this.deleteError = err?.error?.message || 'Delete failed.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  warehousesMatch(): boolean {
    const src = this.form.get('sourceWarehouseId')?.value;
    const dest = this.form.get('destinationWarehouseId')?.value;
    return src != null && dest != null && src === dest;
  }

  totalQuantity(): number {
    return this.lines.controls.reduce((sum, l) => sum + (Number(l.get('quantity')?.value) || 0), 0);
  }

  meta(line: AbstractControl) {
    return line.getRawValue();
  }

  formatNumber(n: number): string {
    return new Intl.NumberFormat('en-BD', { maximumFractionDigits: 4 }).format(n || 0);
  }
}
