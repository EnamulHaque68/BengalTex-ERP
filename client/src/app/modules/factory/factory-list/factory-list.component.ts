import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { FactoryService, CompanyService } from '../../../services/company.service';
import { FactoryListItemDto, FactoryDto, CompanyDto } from '../../../models/company.models';

@Component({
  selector: 'app-factory-list',
  standalone: false,
  templateUrl: './factory-list.component.html',
  styleUrl: './factory-list.component.scss'
})
export class FactoryListComponent implements OnInit {
  factories: FactoryListItemDto[] = [];
  loading = false;
  includeInactive = false;

  // Dialog state
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete confirm
  deleteDialogVisible = false;
  deletingFactory: FactoryListItemDto | null = null;
  deleting = false;

  companyId: number | null = null;

  constructor(
    private factoryService: FactoryService,
    private companyService: CompanyService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadCompanyId();
    this.loadFactories();
  }

  private buildForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      code: ['', [Validators.required, Validators.maxLength(20), Validators.pattern(/^[A-Z0-9_-]+$/)]],
      addressLine1: ['', [Validators.required, Validators.maxLength(300)]],
      addressLine2: ['', Validators.maxLength(300)],
      city: ['', [Validators.required, Validators.maxLength(100)]],
      district: ['', Validators.maxLength(100)],
      postalCode: ['', Validators.maxLength(20)],
      phone: ['', Validators.maxLength(30)],
      isActive: [true],
      geoFenceLat: [null],
      geoFenceLng: [null],
      geoFenceRadiusMeters: [null]
    });
  }

  private loadCompanyId(): void {
    this.companyService.getCompany().subscribe({
      next: (res) => {
        if (res.success && res.data) this.companyId = res.data.id;
      }
    });
  }

  loadFactories(): void {
    this.loading = true;
    this.factoryService.getAll(this.includeInactive).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          this.factories = res.data ?? [];
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => {
          this.loading = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.reset({ isActive: true });
    this.dialogVisible = true;
  }

  openEdit(id: number): void {
    this.dialogMode = 'edit';
    this.editingId = id;
    this.dialogError = '';
    this.dialogVisible = true;

    this.factoryService.getById(id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          const f = res.data;
          this.form.patchValue({
            name: f.name, code: f.code,
            addressLine1: f.addressLine1, addressLine2: f.addressLine2 ?? '',
            city: f.city, district: f.district ?? '', postalCode: f.postalCode ?? '',
            phone: f.phone ?? '', isActive: f.isActive,
            geoFenceLat: f.geoFenceLat, geoFenceLng: f.geoFenceLng,
            geoFenceRadiusMeters: f.geoFenceRadiusMeters
          });
        }
      }
    });
  }

  saveFactory(): void {
    if (this.form.invalid || this.dialogSaving) return;

    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.value;
    const payload = {
      name: v.name,
      code: (v.code as string).toUpperCase(),
      addressLine1: v.addressLine1,
      addressLine2: v.addressLine2 || null,
      city: v.city,
      district: v.district || null,
      postalCode: v.postalCode || null,
      phone: v.phone || null,
      isActive: v.isActive,
      geoFenceLat: v.geoFenceLat || null,
      geoFenceLng: v.geoFenceLng || null,
      geoFenceRadiusMeters: v.geoFenceRadiusMeters || null
    };

    const req$ = this.dialogMode === 'create'
      ? this.factoryService.create({ ...payload, companyId: this.companyId! })
      : this.factoryService.update(this.editingId!, payload);

    req$.subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.dialogSaving = false;
          if (res.success) {
            this.dialogVisible = false;
            this.loadFactories();
          } else {
            this.dialogError = res.message || 'Save failed.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.dialogSaving = false;
          this.dialogError = err?.error?.message || 'Save failed.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  confirmDelete(factory: FactoryListItemDto): void {
    this.deletingFactory = factory;
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingFactory) return;
    this.deleting = true;

    this.factoryService.delete(this.deletingFactory.id).subscribe({
      next: () => {
        this.zone.run(() => {
          this.deleting = false;
          this.deleteDialogVisible = false;
          this.deletingFactory = null;
          this.loadFactories();
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => {
          this.deleting = false;
          this.cdr.detectChanges();
        });
      }
    });
  }
}
