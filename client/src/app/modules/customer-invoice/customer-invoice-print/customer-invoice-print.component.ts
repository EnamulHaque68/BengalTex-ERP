import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerInvoiceService } from '../../../services/customer-invoice.service';
import { CompanyService } from '../../../services/company.service';
import { CustomerInvoiceDto } from '../../../models/customer-invoice.models';
import { CompanyDto } from '../../../models/company.models';
import { numberToWords } from '../../../shared/number-to-words.util';

@Component({
  selector: 'app-customer-invoice-print',
  standalone: false,
  templateUrl: './customer-invoice-print.component.html',
  styleUrl: './customer-invoice-print.component.scss'
})
export class CustomerInvoicePrintComponent implements OnInit {
  get logoSrc(): string { return this.companySvc.logoUrl(); }
  loading = false;
  invoice: CustomerInvoiceDto | null = null;
  company: CompanyDto | null = null;

  constructor(
    private svc: CustomerInvoiceService,
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
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.invoice = res.data; this.tryComplete(); })
    });
    this.companySvc.getCompany().subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.company = res.data; this.tryComplete(); })
    });
  }
  private tryComplete(): void { if (this.invoice && this.company) { this.loading = false; this.cdr.detectChanges(); } }

  back(): void { this.router.navigate(['/customer-invoices']); }
  print(): void { window.print(); }

  formatCurrency(amount: number, code = 'BDT'): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: code, maximumFractionDigits: 2 }).format(amount || 0);
  }
  amountInWords(amount: number, currencyName = 'Taka'): string {
    return numberToWords(amount) + ' ' + currencyName + ' Only';
  }
  vatRateText(rate: number): string { return (rate * 100).toFixed(0) + '%'; }
}
