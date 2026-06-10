import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CustomerService } from '../../../services/customer.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  CUSTOMER_CATEGORIES,
  CustomerCategoryName,
  CustomerListItemDto
} from '../../../models/customer.models';

@Component({
  selector: 'app-customer-list',
  standalone: false,
  templateUrl: './customer-list.component.html',
  styleUrl: './customer-list.component.scss'
})
export class CustomerListComponent implements OnInit {

  customers: CustomerListItemDto[] = [];
  loading = false;
  totalCount = 0;
  includeInactive = false;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  categories = CUSTOMER_CATEGORIES;

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingCustomer: CustomerListItemDto | null = null;
  deleting = false;
  deleteError = '';

  constructor(
    private customerService: CustomerService,
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
      code: ['', [
        Validators.maxLength(50),
        Validators.pattern(/^[A-Z0-9/_-]*$/)
      ]],
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

      category: ['B' as CustomerCategoryName, Validators.required],
      creditLimit: [0, [Validators.required, Validators.min(0)]],
      creditPeriodDays: [0, [Validators.required, Validators.min(0), Validators.max(365)]],
      isExport: [false],

      notes: ['', Validators.maxLength(2000)],
      isActive: [true]
    });
  }

  load(): void {
    this.loading = true;
    this.customerService.getAll(this.parameters, this.includeInactive).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.customers = res.data.items;
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
      category: 'B',
      creditLimit: 0,
      creditPeriodDays: 0,
      isExport: false,
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

    this.customerService.getById(id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const c = res.data;
            this.form.patchValue({
              code: c.code,
              name: c.name,
              contactPerson: c.contactPerson ?? '',
              phone: c.phone ?? '',
              email: c.email ?? '',
              website: c.website ?? '',
              addressLine1: c.addressLine1,
              addressLine2: c.addressLine2 ?? '',
              city: c.city,
              district: c.district ?? '',
              postalCode: c.postalCode ?? '',
              country: c.country,
              binNumber: c.binNumber ?? '',
              vatNumber: c.vatNumber ?? '',
              tinNumber: c.tinNumber ?? '',
              category: c.category,
              creditLimit: c.creditLimit,
              creditPeriodDays: c.creditPeriodDays,
              isExport: c.isExport,
              notes: c.notes ?? '',
              isActive: c.isActive
            });
            // Code is identity — not editable
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
      category: v.category as CustomerCategoryName,
      creditLimit: Number(v.creditLimit) || 0,
      creditPeriodDays: Number(v.creditPeriodDays) || 0,
      isExport: !!v.isExport,
      notes: v.notes || null
    };

    if (this.dialogMode === 'create') {
      this.customerService.create({
        ...baseFields,
        code: v.code ? (v.code as string).toUpperCase() : null
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.customerService.update(this.editingId, {
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

  confirmDelete(customer: CustomerListItemDto): void {
    this.deletingCustomer = customer;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingCustomer || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.customerService.delete(this.deletingCustomer.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingCustomer = null;
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
      maximumFractionDigits: 0
    }).format(amount);
  }
}
