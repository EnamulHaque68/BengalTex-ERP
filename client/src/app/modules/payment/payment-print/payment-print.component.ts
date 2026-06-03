import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentService } from '../../../services/payment.service';
import { CompanyService } from '../../../services/company.service';
import { PaymentDto } from '../../../models/payment.models';
import { CompanyDto } from '../../../models/company.models';
import { numberToWords } from '../../../shared/number-to-words.util';

@Component({
  selector: 'app-payment-print',
  standalone: false,
  templateUrl: './payment-print.component.html',
  styleUrl: './payment-print.component.scss'
})
export class PaymentPrintComponent implements OnInit {
  loading = false;
  pay: PaymentDto | null = null;
  company: CompanyDto | null = null;

  constructor(
    private svc: PaymentService,
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
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.pay = res.data; this.tryComplete(); })
    });
    this.companySvc.getCompany().subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.company = res.data; this.tryComplete(); })
    });
  }
  private tryComplete(): void { if (this.pay && this.company) { this.loading = false; this.cdr.detectChanges(); } }

  back(): void { this.router.navigate(['/payments']); }
  print(): void { window.print(); }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }
  amountInWords(amount: number): string { return numberToWords(amount) + ' Taka Only'; }
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
