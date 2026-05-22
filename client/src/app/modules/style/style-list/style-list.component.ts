import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { StyleService } from '../../../services/style.service';
import { CustomerService } from '../../../services/customer.service';
import { ProductService } from '../../../services/product.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { StyleListItemDto, STYLE_STATUSES } from '../../../models/style.models';
import { CustomerListItemDto } from '../../../models/customer.models';
import { ProductListItemDto } from '../../../models/product.models';

@Component({
  selector: 'app-style-list',
  standalone: false,
  templateUrl: './style-list.component.html',
  styleUrl: './style-list.component.scss'
})
export class StyleListComponent implements OnInit {

  styles: StyleListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  includeInactive = false;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = STYLE_STATUSES;
  customers: CustomerListItemDto[] = [];
  products: ProductListItemDto[] = [];

  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  deleteDialogVisible = false;
  deletingStyle: StyleListItemDto | null = null;
  deleting = false;
  deleteError = '';

  constructor(
    private service: StyleService,
    private customerService: CustomerService,
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

  private buildForm(): void {
    this.form = this.fb.group({
      code: [''],
      styleName: ['', [Validators.required, Validators.maxLength(200)]],
      buyerId: [null as number | null, Validators.required],
      productId: [null as number | null],
      buyerStyleRef: ['', Validators.maxLength(100)],
      season: ['', Validators.maxLength(50)],
      status: ['Development', Validators.required],
      description: ['', Validators.maxLength(2000)],
      notes: ['', Validators.maxLength(2000)],
      isActive: [true]
    });
  }

  private loadDropdowns(): void {
    this.customerService.getAll({ page: 1, pageSize: 1000, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.customers = res.data.items; this.cdr.detectChanges(); })
    });
    this.productService.getAll({ page: 1, pageSize: 1000, search: '' }, undefined, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.products = res.data.items; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.service.getAll(this.parameters, this.includeInactive, undefined, this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.styles = res.data.items; this.totalCount = res.data.totalCount; }
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
    this.form.reset({
      code: '', styleName: '', buyerId: null, productId: null, buyerStyleRef: '',
      season: '', status: 'Development', description: '', notes: '', isActive: true
    });
    this.dialogVisible = true;
  }

  openEdit(style: StyleListItemDto): void {
    this.dialogMode = 'edit';
    this.editingId = style.id;
    this.dialogError = '';
    this.dialogVisible = true;
    this.service.getById(style.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const s = res.data;
          this.form.patchValue({
            code: s.code, styleName: s.styleName, buyerId: s.buyerId, productId: s.productId,
            buyerStyleRef: s.buyerStyleRef ?? '', season: s.season ?? '', status: s.status,
            description: s.description ?? '', notes: s.notes ?? '', isActive: s.isActive
          });
          this.cdr.detectChanges();
        }
      })
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();
    const common = {
      styleName: v.styleName,
      buyerId: v.buyerId,
      productId: v.productId ?? null,
      buyerStyleRef: (v.buyerStyleRef as string)?.trim() || null,
      season: (v.season as string)?.trim() || null,
      status: v.status,
      description: (v.description as string)?.trim() || null,
      notes: (v.notes as string)?.trim() || null
    };

    if (this.dialogMode === 'create') {
      this.service.create({ ...common, code: (v.code as string)?.trim() || null }).subscribe({
        next: (res) => this.handleSave(res), error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.service.update(this.editingId, { ...common, isActive: v.isActive }).subscribe({
        next: (res) => this.handleSave(res), error: (err) => this.handleError(err)
      });
    }
  }

  private handleSave(res: { success: boolean; message?: string | null }): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      if (res.success) { this.dialogVisible = false; this.load(); }
      else this.dialogError = res.message || 'Save failed.';
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

  confirmDelete(style: StyleListItemDto): void {
    this.deletingStyle = style;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingStyle || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();
    this.service.delete(this.deletingStyle.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deleting = false;
        if (res.success) { this.deleteDialogVisible = false; this.deletingStyle = null; this.load(); }
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
}
