import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RawMaterialService } from '../../../services/raw-material.service';
import { UnitOfMeasureService } from '../../../services/unit-of-measure.service';
import { SupplierService } from '../../../services/supplier.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { MATERIAL_CATEGORIES, RawMaterialListItemDto } from '../../../models/raw-material.models';
import { UnitOfMeasureDto } from '../../../models/master-data.models';
import { SupplierListItemDto } from '../../../models/supplier.models';

@Component({
  selector: 'app-raw-material-list',
  standalone: false,
  templateUrl: './raw-material-list.component.html',
  styleUrl: './raw-material-list.component.scss'
})
export class RawMaterialListComponent implements OnInit {

  materials: RawMaterialListItemDto[] = [];
  loading = false;
  totalCount = 0;
  includeInactive = false;
  filterCategory: string | null = null;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Dropdown sources
  readonly categories = MATERIAL_CATEGORIES;
  units: UnitOfMeasureDto[] = [];
  suppliers: SupplierListItemDto[] = [];

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingMaterial: RawMaterialListItemDto | null = null;
  deleting = false;
  deleteError = '';

  constructor(
    private rawMaterialService: RawMaterialService,
    private uomService: UnitOfMeasureService,
    private supplierService: SupplierService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadDropdowns();
    this.load();
  }

  private buildForm(): void {
    this.form = this.fb.group({
      code: ['', [Validators.maxLength(50), Validators.pattern(/^[A-Z0-9/_-]*$/)]],
      name: ['', [Validators.required, Validators.maxLength(200)]],
      specification: ['', Validators.maxLength(1000)],
      category: [null as string | null, Validators.required],
      unitOfMeasureId: [null as number | null, Validators.required],
      minimumStockLevel: [0, [Validators.required, Validators.min(0)]],
      openingStock: [0, [Validators.required, Validators.min(0)]],
      standardCost: [0, [Validators.required, Validators.min(0)]],
      preferredSupplierId: [null as number | null],
      notes: ['', Validators.maxLength(2000)],
      isActive: [true]
    });
  }

  private loadDropdowns(): void {
    this.uomService.getAll(false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success) this.units = res.data ?? [];
          this.cdr.detectChanges();
        });
      }
    });
    this.supplierService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.suppliers = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.rawMaterialService.getAll(this.parameters, this.filterCategory ?? undefined, this.includeInactive).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.materials = res.data.items;
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

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.reset({
      code: '', name: '', specification: '',
      category: this.filterCategory ?? this.categories[0]?.value ?? null,
      unitOfMeasureId: this.units[0]?.id ?? null,
      minimumStockLevel: 0, openingStock: 0, standardCost: 0,
      preferredSupplierId: null, notes: '', isActive: true
    });
    this.form.get('code')?.enable();
    this.dialogVisible = true;
  }

  openEdit(id: number): void {
    this.dialogMode = 'edit';
    this.editingId = id;
    this.dialogError = '';
    this.dialogVisible = true;

    this.rawMaterialService.getById(id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const m = res.data;
            this.form.patchValue({
              code: m.code,
              name: m.name,
              specification: m.specification ?? '',
              category: m.category,
              unitOfMeasureId: m.unitOfMeasureId,
              minimumStockLevel: m.minimumStockLevel,
              openingStock: m.openingStock,
              standardCost: m.standardCost,
              preferredSupplierId: m.preferredSupplierId,
              notes: m.notes ?? '',
              isActive: m.isActive
            });
            this.form.get('code')?.disable();
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;

    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();

    const baseFields = {
      name: v.name,
      specification: v.specification || null,
      category: v.category,
      unitOfMeasureId: v.unitOfMeasureId,
      minimumStockLevel: Number(v.minimumStockLevel) || 0,
      openingStock: Number(v.openingStock) || 0,
      standardCost: Number(v.standardCost) || 0,
      preferredSupplierId: v.preferredSupplierId ?? null,
      notes: v.notes || null
    };

    if (this.dialogMode === 'create') {
      this.rawMaterialService.create({
        ...baseFields,
        code: v.code ? (v.code as string).toUpperCase() : null
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.rawMaterialService.update(this.editingId, {
        ...baseFields,
        isActive: v.isActive
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

  confirmDelete(material: RawMaterialListItemDto): void {
    this.deletingMaterial = material;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingMaterial || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.rawMaterialService.delete(this.deletingMaterial.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingMaterial = null;
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

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency',
      currency: 'BDT',
      maximumFractionDigits: 2
    }).format(amount);
  }

  categoryLabel(value: string): string {
    return this.categories.find(c => c.value === value)?.label ?? value;
  }
}
