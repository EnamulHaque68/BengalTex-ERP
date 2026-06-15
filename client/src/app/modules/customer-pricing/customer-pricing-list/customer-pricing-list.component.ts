import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CustomerPricingService } from '../../../services/customer-pricing.service';
import { CustomerService } from '../../../services/customer.service';
import { ProductService } from '../../../services/product.service';
import { CustomerProductPriceDto } from '../../../models/customer-pricing.models';
import { CustomerListItemDto } from '../../../models/customer.models';
import { ProductListItemDto } from '../../../models/product.models';

@Component({
  selector: 'app-customer-pricing-list',
  standalone: false,
  templateUrl: './customer-pricing-list.component.html',
  styleUrl: './customer-pricing-list.component.scss'
})
export class CustomerPricingListComponent implements OnInit {
  customers: CustomerListItemDto[] = [];
  products: ProductListItemDto[] = [];
  selectedCustomerId: number | null = null;

  prices: CustomerProductPriceDto[] = [];
  loading = false;
  error = '';

  dialogVisible = false;
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteTarget: CustomerProductPriceDto | null = null;
  deleteVisible = false;
  deleting = false;

  constructor(
    private pricingService: CustomerPricingService,
    private customerService: CustomerService,
    private productService: ProductService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      productId: [null as number | null, Validators.required],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
      buyerProductCode: ['', Validators.maxLength(50)],
      buyerProductName: ['', Validators.maxLength(200)],
      isActive: [true],
      notes: ['', Validators.maxLength(1000)]
    });
    this.loadCustomers();
    this.loadProducts();
  }

  private loadCustomers(): void {
    this.customerService.getAll({ page: 1, pageSize: 1000, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.customers = res.data.items;
        this.cdr.detectChanges();
      })
    });
  }

  private loadProducts(): void {
    this.productService.getAll({ page: 1, pageSize: 1000, search: '' }).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.products = res.data.items;
        this.cdr.detectChanges();
      })
    });
  }

  onCustomerChange(): void {
    this.prices = [];
    if (this.selectedCustomerId) this.loadPrices();
  }

  loadPrices(): void {
    if (!this.selectedCustomerId) return;
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();
    this.pricingService.getForCustomer(this.selectedCustomerId, true).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.prices = res.data;
        else this.error = res.message || 'Failed to load prices.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loading = false;
        this.error = err?.error?.message || 'Failed to load prices.';
        this.cdr.detectChanges();
      })
    });
  }

  openCreate(): void {
    this.editingId = null;
    this.dialogError = '';
    this.form.reset({ productId: null, unitPrice: 0, buyerProductCode: '', buyerProductName: '', isActive: true, notes: '' });
    this.form.get('productId')?.enable();
    this.dialogVisible = true;
  }

  openEdit(p: CustomerProductPriceDto): void {
    this.editingId = p.id;
    this.dialogError = '';
    this.form.reset({
      productId: p.productId, unitPrice: p.unitPrice,
      buyerProductCode: p.buyerProductCode ?? '', buyerProductName: p.buyerProductName ?? '',
      isActive: p.isActive, notes: p.notes ?? ''
    });
    this.form.get('productId')?.disable();   // product is fixed once a price row exists
    this.dialogVisible = true;
  }

  /** Products not yet priced for this customer (for the create picker). */
  get availableProducts(): ProductListItemDto[] {
    if (this.editingId) return this.products;
    const taken = new Set(this.prices.map(p => p.productId));
    return this.products.filter(p => !taken.has(p.id));
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving || !this.selectedCustomerId) return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();
    const v = this.form.getRawValue();

    const done = {
      next: (res: any) => this.zone.run(() => {
        this.dialogSaving = false;
        if (res.success) { this.dialogVisible = false; this.loadPrices(); }
        else this.dialogError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err: any) => this.zone.run(() => {
        this.dialogSaving = false;
        this.dialogError = err?.error?.message || 'Save failed.';
        this.cdr.detectChanges();
      })
    };

    const buyerCode = (v.buyerProductCode as string)?.trim() || null;
    const buyerName = (v.buyerProductName as string)?.trim() || null;
    const notes = (v.notes as string)?.trim() || null;

    if (this.editingId) {
      this.pricingService.update({
        id: this.editingId, unitPrice: Number(v.unitPrice),
        buyerProductCode: buyerCode, buyerProductName: buyerName, isActive: !!v.isActive, notes
      }).subscribe(done);
    } else {
      this.pricingService.create({
        customerId: this.selectedCustomerId, productId: v.productId, unitPrice: Number(v.unitPrice),
        buyerProductCode: buyerCode, buyerProductName: buyerName, notes
      }).subscribe(done);
    }
  }

  confirmDelete(p: CustomerProductPriceDto): void { this.deleteTarget = p; this.deleteVisible = true; }

  doDelete(): void {
    if (!this.deleteTarget) return;
    this.deleting = true;
    this.cdr.detectChanges();
    this.pricingService.delete(this.deleteTarget.id).subscribe({
      next: () => this.zone.run(() => {
        this.deleting = false; this.deleteVisible = false; this.deleteTarget = null;
        this.loadPrices();
      }),
      error: () => this.zone.run(() => { this.deleting = false; this.cdr.detectChanges(); })
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }
}
