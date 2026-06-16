import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ProductService } from '../../../services/product.service';
import { ProductCategoryService } from '../../../services/product-category.service';
import { UnitOfMeasureService } from '../../../services/unit-of-measure.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { ProductCategoryDto, ProductListItemDto, ProductPriceHistoryDto } from '../../../models/product.models';
import { UnitOfMeasureDto } from '../../../models/master-data.models';

@Component({
  selector: 'app-product-list',
  standalone: false,
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss'
})
export class ProductListComponent implements OnInit {

  products: ProductListItemDto[] = [];
  loading = false;
  totalCount = 0;
  includeInactive = false;
  filterCategoryId: number | null = null;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Dropdown sources
  categories: ProductCategoryDto[] = [];
  units: UnitOfMeasureDto[] = [];

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingProduct: ProductListItemDto | null = null;
  deleting = false;
  deleteError = '';

  // Price history
  priceHistoryVisible = false;
  priceHistoryLoading = false;
  priceHistoryProduct: ProductListItemDto | null = null;
  priceHistory: ProductPriceHistoryDto[] = [];

  constructor(
    private productService: ProductService,
    private categoryService: ProductCategoryService,
    private uomService: UnitOfMeasureService,
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
      productCategoryId: [null as number | null, Validators.required],
      unitOfMeasureId: [null as number | null, Validators.required],
      size: ['', Validators.maxLength(50)],
      color: ['', Validators.maxLength(50)],
      material: ['', Validators.maxLength(100)],
      hsCode: ['', Validators.maxLength(20)],
      salesPrice: [0, [Validators.required, Validators.min(0)]],
      reorderLevel: [0, [Validators.required, Validators.min(0)]],
      isStockItem: [true],
      imageUrl: ['', Validators.maxLength(500)],
      notes: ['', Validators.maxLength(2000)],
      isActive: [true]
    });
  }

  private loadDropdowns(): void {
    this.categoryService.getAll(false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success) this.categories = res.data ?? [];
          this.cdr.detectChanges();
        });
      }
    });
    this.uomService.getAll(false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success) this.units = res.data ?? [];
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.productService.getAll(this.parameters, this.filterCategoryId ?? undefined, this.includeInactive).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.products = res.data.items;
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
      productCategoryId: this.filterCategoryId ?? this.categories[0]?.id ?? null,
      unitOfMeasureId: this.units[0]?.id ?? null,
      size: '', color: '', material: '', hsCode: '',
      salesPrice: 0, reorderLevel: 0, isStockItem: true,
      imageUrl: '', notes: '', isActive: true
    });
    this.form.get('code')?.enable();
    this.dialogVisible = true;
  }

  openEdit(id: number): void {
    this.dialogMode = 'edit';
    this.editingId = id;
    this.dialogError = '';
    this.dialogVisible = true;

    this.productService.getById(id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const p = res.data;
            this.form.patchValue({
              code: p.code,
              name: p.name,
              specification: p.specification ?? '',
              productCategoryId: p.productCategoryId,
              unitOfMeasureId: p.unitOfMeasureId,
              size: p.size ?? '',
              color: p.color ?? '',
              material: p.material ?? '',
              hsCode: p.hsCode ?? '',
              salesPrice: p.salesPrice,
              reorderLevel: p.reorderLevel,
              isStockItem: p.isStockItem,
              imageUrl: p.imageUrl ?? '',
              notes: p.notes ?? '',
              isActive: p.isActive
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
      productCategoryId: v.productCategoryId,
      unitOfMeasureId: v.unitOfMeasureId,
      size: v.size || null,
      color: v.color || null,
      material: v.material || null,
      hsCode: v.hsCode ? (v.hsCode as string).trim() : null,
      salesPrice: Number(v.salesPrice) || 0,
      reorderLevel: Number(v.reorderLevel) || 0,
      isStockItem: v.isStockItem,
      imageUrl: v.imageUrl || null,
      notes: v.notes || null
    };

    if (this.dialogMode === 'create') {
      this.productService.create({
        ...baseFields,
        code: v.code ? (v.code as string).toUpperCase() : null
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.productService.update(this.editingId, {
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

  confirmDelete(product: ProductListItemDto): void {
    this.deletingProduct = product;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  openPriceHistory(product: ProductListItemDto): void {
    this.priceHistoryProduct = product;
    this.priceHistory = [];
    this.priceHistoryLoading = true;
    this.priceHistoryVisible = true;
    this.cdr.detectChanges();
    this.productService.getPriceHistory(product.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.priceHistoryLoading = false;
        if (res.success && res.data) this.priceHistory = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.priceHistoryLoading = false; this.cdr.detectChanges(); })
    });
  }

  doDelete(): void {
    if (!this.deletingProduct || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.productService.delete(this.deletingProduct.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingProduct = null;
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
}
