import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CurrencyService } from '../../../services/currency.service';
import { CurrencyDto } from '../../../models/master-data.models';

@Component({
  selector: 'app-currency-list',
  standalone: false,
  templateUrl: './currency-list.component.html',
  styleUrl: './currency-list.component.scss'
})
export class CurrencyListComponent implements OnInit {

  currencies: CurrencyDto[] = [];
  loading = false;
  includeInactive = false;

  // Create / Edit dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete confirm
  deleteDialogVisible = false;
  deletingCurrency: CurrencyDto | null = null;
  deleting = false;
  deleteError = '';

  constructor(
    private currencyService: CurrencyService,
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
        Validators.required,
        Validators.pattern(/^[A-Z]{3}$/)
      ]],
      name: ['', [Validators.required, Validators.maxLength(100)]],
      symbol: ['', [Validators.required, Validators.maxLength(10)]],
      exchangeRateToBase: [1, [Validators.required, Validators.min(0.000001)]],
      isBaseCurrency: [false],
      isActive: [true]
    });

    // Auto-set exchangeRate to 1 when isBaseCurrency toggles ON
    this.form.get('isBaseCurrency')?.valueChanges.subscribe(isBase => {
      if (isBase) this.form.get('exchangeRateToBase')?.setValue(1);
    });
  }

  load(): void {
    this.loading = true;
    this.currencyService.getAll(this.includeInactive).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          this.currencies = res.data ?? [];
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); });
      }
    });
  }

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.reset({
      code: '', name: '', symbol: '',
      exchangeRateToBase: 1, isBaseCurrency: false, isActive: true
    });
    this.form.get('code')?.enable();
    this.dialogVisible = true;
  }

  openEdit(id: number): void {
    this.dialogMode = 'edit';
    this.editingId = id;
    this.dialogError = '';
    this.dialogVisible = true;

    this.currencyService.getById(id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const c = res.data;
            this.form.patchValue({
              code: c.code,
              name: c.name,
              symbol: c.symbol,
              exchangeRateToBase: c.exchangeRateToBase,
              isBaseCurrency: c.isBaseCurrency,
              isActive: c.isActive
            });
            // Code is identity — not editable in edit mode
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

    if (this.dialogMode === 'create') {
      this.currencyService.create({
        code: (v.code as string).toUpperCase(),
        name: v.name,
        symbol: v.symbol,
        exchangeRateToBase: v.exchangeRateToBase,
        isBaseCurrency: v.isBaseCurrency
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.currencyService.update(this.editingId, {
        name: v.name,
        symbol: v.symbol,
        exchangeRateToBase: v.exchangeRateToBase,
        isBaseCurrency: v.isBaseCurrency,
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

  confirmDelete(currency: CurrencyDto): void {
    this.deletingCurrency = currency;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingCurrency || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.currencyService.delete(this.deletingCurrency.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingCurrency = null;
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
