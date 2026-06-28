import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ReceiptService } from '../../../services/receipt.service';
import { CompanyService } from '../../../services/company.service';
import { ReceiptDto } from '../../../models/receipt.models';
import { CompanyDto } from '../../../models/company.models';
import { numberToWords } from '../../../shared/number-to-words.util';

@Component({
  selector: 'app-receipt-print',
  standalone: false,
  templateUrl: './receipt-print.component.html',
  styleUrl: './receipt-print.component.scss'
})
export class ReceiptPrintComponent implements OnInit {
  get logoSrc(): string { return this.companySvc.logoUrl(); }
  loading = false;
  receipt: ReceiptDto | null = null;
  company: CompanyDto | null = null;

  constructor(
    private svc: ReceiptService,
    private companySvc: CompanyService,
    private route: ActivatedRoute,
    private router: Router,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loading = true;
    this.svc.getById(id).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.receipt = res.data; this.tryComplete(); })
    });
    this.companySvc.getCompany().subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.company = res.data; this.tryComplete(); })
    });
  }
  private tryComplete(): void { if (this.receipt && this.company) { this.loading = false; this.cdr.detectChanges(); } }

  back(): void { this.router.navigate(['/receipts']); }
  print(): void { window.print(); }

  /** Major / minor unit words per currency, for the "amount in words" line. */
  private static readonly UNITS: Record<string, { major: string; minor: string }> = {
    BDT: { major: 'Taka', minor: 'Paisa' },
    USD: { major: 'US Dollars', minor: 'Cents' },
    EUR: { major: 'Euros', minor: 'Cents' },
    GBP: { major: 'Pounds', minor: 'Pence' },
    INR: { major: 'Rupees', minor: 'Paisa' }
  };

  /** True when the receipt is in a foreign (non-base) currency — drives the FX info block. */
  get isForeign(): boolean {
    return !!this.receipt && !!this.receipt.currencyCode && this.receipt.currencyCode !== 'BDT';
  }

  /** Format in the receipt's own currency (the primary amount indicator). */
  formatMoney(amount: number, code: string | null | undefined): string {
    const c = code || 'BDT';
    try {
      return new Intl.NumberFormat('en-US', { style: 'currency', currency: c, maximumFractionDigits: 2 }).format(amount || 0);
    } catch {
      return `${(amount || 0).toLocaleString('en-US', { maximumFractionDigits: 2 })} ${c}`;
    }
  }

  /** Base-currency (BDT) formatting — used only for the FX "base amount" info line. */
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }

  amountInWords(amount: number, code: string | null | undefined): string {
    const unit = ReceiptPrintComponent.UNITS[(code || 'BDT').toUpperCase()] ?? { major: (code || 'BDT'), minor: 'Cents' };
    return `${numberToWords(amount, unit.minor)} ${unit.major} Only`;
  }
  methodLabel(m: string): string {
    switch (m) {
      case 'Cash': return 'Cash';
      case 'BankTransfer': return 'Bank Transfer';
      case 'Cheque': return 'Cheque';
      case 'MobileBanking': return 'Mobile Banking (bKash / Nagad)';
      default: return m;
    }
  }
}
