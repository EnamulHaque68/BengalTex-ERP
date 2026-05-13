import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CompanyService } from '../../../services/company.service';
import { CompanyDto } from '../../../models/company.models';

@Component({
  selector: 'app-company-settings',
  standalone: false,
  templateUrl: './company-settings.component.html',
  styleUrl: './company-settings.component.scss'
})
export class CompanySettingsComponent implements OnInit {
  form!: FormGroup;
  loading = false;
  saving = false;
  errorMessage = '';
  successMessage = '';
  companyExists = false;

  constructor(
    private fb: FormBuilder,
    private companyService: CompanyService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadCompany();
  }

  private buildForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      shortName: ['', [Validators.required, Validators.maxLength(50)]],
      registrationNumber: ['', Validators.maxLength(100)],
      taxNumber: ['', Validators.maxLength(50)],
      addressLine1: ['', [Validators.required, Validators.maxLength(300)]],
      addressLine2: ['', Validators.maxLength(300)],
      city: ['', [Validators.required, Validators.maxLength(100)]],
      district: ['', [Validators.required, Validators.maxLength(100)]],
      postalCode: ['', Validators.maxLength(20)],
      country: ['Bangladesh', [Validators.required, Validators.maxLength(100)]],
      phone: ['', Validators.maxLength(30)],
      email: ['', [Validators.email, Validators.maxLength(200)]],
      website: ['', Validators.maxLength(200)],
      logoUrl: ['', Validators.maxLength(500)]
    });
  }

  private loadCompany(): void {
    this.loading = true;
    this.companyService.getCompany().subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.companyExists = true;
            this.patchForm(res.data);
          }
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

  private patchForm(dto: CompanyDto): void {
    this.form.patchValue({
      name: dto.name,
      shortName: dto.shortName,
      registrationNumber: dto.registrationNumber ?? '',
      taxNumber: dto.taxNumber ?? '',
      addressLine1: dto.addressLine1,
      addressLine2: dto.addressLine2 ?? '',
      city: dto.city,
      district: dto.district,
      postalCode: dto.postalCode ?? '',
      country: dto.country,
      phone: dto.phone ?? '',
      email: dto.email ?? '',
      website: dto.website ?? '',
      logoUrl: dto.logoUrl ?? ''
    });
  }

  save(): void {
    if (this.form.invalid || this.saving) return;

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    const val = this.form.value;
    this.companyService.updateCompany({
      name: val.name,
      shortName: val.shortName,
      registrationNumber: val.registrationNumber || null,
      taxNumber: val.taxNumber || null,
      addressLine1: val.addressLine1,
      addressLine2: val.addressLine2 || null,
      city: val.city,
      district: val.district,
      postalCode: val.postalCode || null,
      country: val.country,
      phone: val.phone || null,
      email: val.email || null,
      website: val.website || null,
      logoUrl: val.logoUrl || null
    }).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.saving = false;
          if (res.success) {
            this.companyExists = true;
            this.successMessage = res.message || 'Company profile saved.';
          } else {
            this.errorMessage = res.message || 'Save failed.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.saving = false;
          this.errorMessage = err?.error?.message || 'Save failed. Please try again.';
          this.cdr.detectChanges();
        });
      }
    });
  }
}
