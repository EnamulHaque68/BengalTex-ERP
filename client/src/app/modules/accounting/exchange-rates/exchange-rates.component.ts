import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ExchangeRateService } from '../../../services/exchange-rate.service';
import { CurrencyService } from '../../../services/currency.service';
import { AuthService } from '../../../services/auth.service';
import { ExchangeRateDto } from '../../../models/exchange-rate.models';
import { CurrencyDto } from '../../../models/master-data.models';
import { apiErrorMessage } from '../../../shared/utils/http-error.util';

/**
 * Phase A6c — dated exchange-rate history. Unlike the currency master's single "current" rate,
 * these dated rows let a rate be resolved as-of any date (source for month-end FC revaluation).
 */
@Component({
  selector: 'app-exchange-rates',
  standalone: false,
  templateUrl: './exchange-rates.component.html',
  styleUrl: './exchange-rates.component.scss'
})
export class ExchangeRatesComponent implements OnInit {
  loading = false;
  canManage = false;
  actionError = '';
  actionMessage = '';

  rates: ExchangeRateDto[] = [];
  currencies: CurrencyDto[] = [];
  filterCurrencyId: number | null = null;

  dialogVisible = false;
  saving = false;
  dialogError = '';
  currencyId: number | null = null;
  rateDate = '';
  rate = 0;
  source = '';

  constructor(
    private svc: ExchangeRateService,
    private currencyService: CurrencyService,
    private auth: AuthService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('Accounting.CloseBooks');
    this.currencyService.getAll(false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.currencies = res.data.filter(c => !c.isBaseCurrency); this.cdr.detectChanges(); })
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.filterCurrencyId ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.rates = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  openCreate(): void {
    this.currencyId = this.filterCurrencyId ?? (this.currencies[0]?.id ?? null);
    this.rateDate = new Date().toISOString().slice(0, 10);
    this.rate = 0; this.source = ''; this.dialogError = '';
    this.dialogVisible = true;
  }

  save(): void {
    if (this.saving || !this.currencyId || this.rate <= 0 || !this.rateDate) return;
    this.saving = true; this.dialogError = '';
    this.svc.set({ currencyId: this.currencyId, rateDate: this.rateDate, rate: Number(this.rate) || 0, source: this.source.trim() || null }).subscribe({
      next: (res) => this.zone.run(() => {
        this.saving = false;
        if (res.success) { this.dialogVisible = false; this.actionMessage = res.message || 'Rate saved.'; this.load(); }
        else this.dialogError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.saving = false; this.dialogError = apiErrorMessage(err, 'Save failed.'); this.cdr.detectChanges(); })
    });
  }
}
