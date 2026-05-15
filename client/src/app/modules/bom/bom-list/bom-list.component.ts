import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BomService } from '../../../services/bom.service';
import { ProductService } from '../../../services/product.service';
import { RawMaterialService } from '../../../services/raw-material.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { BOM_STATUSES, BomListItemDto } from '../../../models/bom.models';
import { ProductListItemDto } from '../../../models/product.models';
import { RawMaterialListItemDto } from '../../../models/raw-material.models';

@Component({
  selector: 'app-bom-list',
  standalone: false,
  templateUrl: './bom-list.component.html',
  styleUrl: './bom-list.component.scss'
})
export class BomListComponent implements OnInit {

  boms: BomListItemDto[] = [];
  loading = false;
  totalCount = 0;
  activeOnly = false;
  filterProductId: number | null = null;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Dropdown sources
  readonly statuses = BOM_STATUSES;
  products: ProductListItemDto[] = [];
  rawMaterials: RawMaterialListItemDto[] = [];

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingBom: BomListItemDto | null = null;
  deleting = false;
  deleteError = '';

  // Row action (approve / activate) in-flight id
  rowActionId: number | null = null;

  constructor(
    private bomService: BomService,
    private productService: ProductService,
    private rawMaterialService: RawMaterialService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadDropdowns();
    this.load();
  }

  // ─── Form ────────────────────────────────────────────────────────────────

  private buildForm(): void {
    this.form = this.fb.group({
      productId: [null as number | null, Validators.required],
      name: ['', Validators.maxLength(200)],
      outputQuantity: [1, [Validators.required, Validators.min(0.0001)]],
      effectiveDate: [null as string | null],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  private newLine(
    rawMaterialId: number | null = null,
    quantity = 1,
    wastagePercent = 0,
    lineNotes = ''
  ): FormGroup {
    return this.fb.group({
      rawMaterialId: [rawMaterialId, Validators.required],
      quantity: [quantity, [Validators.required, Validators.min(0.0001)]],
      wastagePercent: [wastagePercent, [Validators.required, Validators.min(0), Validators.max(100)]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  addLine(): void {
    this.lines.push(this.newLine());
  }

  removeLine(index: number): void {
    this.lines.removeAt(index);
  }

  // ─── Line computations (client-side preview) ─────────────────────────────

  rawMaterialById(id: number | null | undefined): RawMaterialListItemDto | undefined {
    return id ? this.rawMaterials.find(r => r.id === id) : undefined;
  }

  lineUomCode(line: AbstractControl): string {
    return this.rawMaterialById(line.get('rawMaterialId')?.value)?.unitOfMeasureCode ?? '—';
  }

  lineUnitCost(line: AbstractControl): number {
    return this.rawMaterialById(line.get('rawMaterialId')?.value)?.standardCost ?? 0;
  }

  lineEffectiveQty(line: AbstractControl): number {
    const qty = Number(line.get('quantity')?.value) || 0;
    const wastage = Number(line.get('wastagePercent')?.value) || 0;
    return qty * (1 + wastage / 100);
  }

  lineCost(line: AbstractControl): number {
    return this.lineEffectiveQty(line) * this.lineUnitCost(line);
  }

  totalCost(): number {
    return this.lines.controls.reduce((sum, l) => sum + this.lineCost(l), 0);
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency',
      currency: 'BDT',
      maximumFractionDigits: 2
    }).format(amount || 0);
  }

  // ─── Data loading ────────────────────────────────────────────────────────

  private loadDropdowns(): void {
    this.productService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.products = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
    this.rawMaterialService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.rawMaterials = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.bomService.getAll(
      this.parameters,
      this.filterProductId ?? undefined,
      this.filterStatus ?? undefined,
      this.activeOnly
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.boms = res.data.items;
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

  // ─── Create / Edit / View dialog ─────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      productId: this.filterProductId ?? null,
      name: '',
      outputQuantity: 1,
      effectiveDate: null,
      notes: ''
    });
    this.addLine();
    this.dialogVisible = true;
  }

  openEdit(bom: BomListItemDto): void {
    this.editingId = bom.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.dialogVisible = true;

    this.bomService.getById(bom.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const b = res.data;
            this.dialogMode = b.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              productId: b.productId,
              name: b.name ?? '',
              outputQuantity: b.outputQuantity,
              effectiveDate: b.effectiveDate ?? null,
              notes: b.notes ?? ''
            });
            b.lines.forEach(l => this.lines.push(
              this.newLine(l.rawMaterialId, l.quantity, l.wastagePercent, l.lineNotes ?? '')
            ));
            // Product is fixed once a BOM exists — its version chain belongs to that product
            this.form.get('productId')?.disable();
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
    const lines = (v.lines as any[]).map(l => ({
      rawMaterialId: l.rawMaterialId,
      quantity: Number(l.quantity) || 0,
      wastagePercent: Number(l.wastagePercent) || 0,
      lineNotes: (l.lineNotes as string)?.trim() || null
    }));

    const baseFields = {
      name: (v.name as string)?.trim() || null,
      outputQuantity: Number(v.outputQuantity) || 0,
      effectiveDate: v.effectiveDate || null,
      notes: (v.notes as string)?.trim() || null,
      lines
    };

    if (this.dialogMode === 'create') {
      this.bomService.create({ productId: v.productId, ...baseFields }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.bomService.update(this.editingId, baseFields).subscribe({
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

  // ─── Row actions: approve / activate ─────────────────────────────────────

  approve(bom: BomListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = bom.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.bomService.approve(bom.id).subscribe({
      next: (res) => this.handleRowAction(res),
      error: (err) => this.handleRowActionError(err)
    });
  }

  activate(bom: BomListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = bom.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.bomService.activate(bom.id).subscribe({
      next: (res) => this.handleRowAction(res),
      error: (err) => this.handleRowActionError(err)
    });
  }

  private handleRowAction(res: { success: boolean; message?: string | null }): void {
    this.zone.run(() => {
      this.rowActionId = null;
      if (res.success) {
        this.actionError = '';
        this.load();
      } else {
        this.actionError = res.message || 'Action failed.';
      }
      this.cdr.detectChanges();
    });
  }

  private handleRowActionError(err: any): void {
    this.zone.run(() => {
      this.rowActionId = null;
      this.actionError = err?.error?.message || 'Action failed.';
      this.cdr.detectChanges();
    });
  }

  // ─── Delete ──────────────────────────────────────────────────────────────

  confirmDelete(bom: BomListItemDto): void {
    this.deletingBom = bom;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingBom || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.bomService.delete(this.deletingBom.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingBom = null;
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
}
