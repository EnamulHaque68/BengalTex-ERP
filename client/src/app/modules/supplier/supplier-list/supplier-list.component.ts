import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SupplierService } from '../../../services/supplier.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { SupplierListItemDto } from '../../../models/supplier.models';

@Component({
  selector: 'app-supplier-list',
  standalone: false,
  templateUrl: './supplier-list.component.html',
  styleUrl: './supplier-list.component.scss'
})
export class SupplierListComponent implements OnInit {

  suppliers: SupplierListItemDto[] = [];
  loading = false;
  totalCount = 0;
  includeInactive = false;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingSupplier: SupplierListItemDto | null = null;
  deleting = false;
  deleteError = '';

  // Star rating helper for the list view
  readonly stars = [1, 2, 3, 4, 5];

  constructor(
    private supplierService: SupplierService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.load();
  }

  private buildForm(): void {
    this.form = this.fb.group({
      code: ['', [Validators.maxLength(50), Validators.pattern(/^[A-Z0-9/_-]*$/)]],
      name: ['', [Validators.required, Validators.maxLength(200)]],
      contactPerson: ['', Validators.maxLength(200)],
      phone: ['', Validators.maxLength(30)],
      email: ['', [Validators.email, Validators.maxLength(200)]],
      website: ['', Validators.maxLength(200)],

      addressLine1: ['', [Validators.required, Validators.maxLength(300)]],
      addressLine2: ['', Validators.maxLength(300)],
      city: ['', [Validators.required, Validators.maxLength(100)]],
      district: ['', Validators.maxLength(100)],
      postalCode: ['', Validators.maxLength(20)],
      country: ['Bangladesh', [Validators.required, Validators.maxLength(100)]],

      binNumber: ['', Validators.maxLength(50)],
      vatNumber: ['', Validators.maxLength(50)],
      tinNumber: ['', Validators.maxLength(50)],

      paymentTermsDays: [0, [Validators.required, Validators.min(0), Validators.max(365)]],

      bankName: ['', Validators.maxLength(100)],
      bankAccountNumber: ['', Validators.maxLength(50)],
      bankBranch: ['', Validators.maxLength(100)],
      bankAccountHolderName: ['', Validators.maxLength(200)],

      rating: [0, [Validators.required, Validators.min(0), Validators.max(5)]],
      notes: ['', Validators.maxLength(2000)],
      isActive: [true]
    });
  }

  load(): void {
    this.loading = true;
    this.supplierService.getAll(this.parameters, this.includeInactive).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.suppliers = res.data.items;
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
      code: '',
      name: '', contactPerson: '', phone: '', email: '', website: '',
      addressLine1: '', addressLine2: '', city: '', district: '', postalCode: '', country: 'Bangladesh',
      binNumber: '', vatNumber: '', tinNumber: '',
      paymentTermsDays: 0,
      bankName: '', bankAccountNumber: '', bankBranch: '', bankAccountHolderName: '',
      rating: 0,
      notes: '',
      isActive: true
    });
    this.form.get('code')?.enable();
    this.dialogVisible = true;
  }

  openEdit(id: number): void {
    this.dialogMode = 'edit';
    this.editingId = id;
    this.dialogError = '';
    this.dialogVisible = true;

    this.supplierService.getById(id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const s = res.data;
            this.form.patchValue({
              code: s.code,
              name: s.name,
              contactPerson: s.contactPerson ?? '',
              phone: s.phone ?? '',
              email: s.email ?? '',
              website: s.website ?? '',
              addressLine1: s.addressLine1,
              addressLine2: s.addressLine2 ?? '',
              city: s.city,
              district: s.district ?? '',
              postalCode: s.postalCode ?? '',
              country: s.country,
              binNumber: s.binNumber ?? '',
              vatNumber: s.vatNumber ?? '',
              tinNumber: s.tinNumber ?? '',
              paymentTermsDays: s.paymentTermsDays,
              bankName: s.bankName ?? '',
              bankAccountNumber: s.bankAccountNumber ?? '',
              bankBranch: s.bankBranch ?? '',
              bankAccountHolderName: s.bankAccountHolderName ?? '',
              rating: s.rating,
              notes: s.notes ?? '',
              isActive: s.isActive
            });
            this.form.get('code')?.disable();
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  setRating(value: number): void {
    this.form.get('rating')?.setValue(value);
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;

    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();

    const baseFields = {
      name: v.name,
      contactPerson: v.contactPerson || null,
      phone: v.phone || null,
      email: v.email || null,
      website: v.website || null,
      addressLine1: v.addressLine1,
      addressLine2: v.addressLine2 || null,
      city: v.city,
      district: v.district || null,
      postalCode: v.postalCode || null,
      country: v.country,
      binNumber: v.binNumber || null,
      vatNumber: v.vatNumber || null,
      tinNumber: v.tinNumber || null,
      paymentTermsDays: Number(v.paymentTermsDays) || 0,
      bankName: v.bankName || null,
      bankAccountNumber: v.bankAccountNumber || null,
      bankBranch: v.bankBranch || null,
      bankAccountHolderName: v.bankAccountHolderName || null,
      rating: Number(v.rating) || 0,
      notes: v.notes || null
    };

    if (this.dialogMode === 'create') {
      this.supplierService.create({
        ...baseFields,
        code: v.code ? (v.code as string).toUpperCase() : null
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.supplierService.update(this.editingId, {
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

  confirmDelete(supplier: SupplierListItemDto): void {
    this.deletingSupplier = supplier;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingSupplier || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.supplierService.delete(this.deletingSupplier.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingSupplier = null;
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
