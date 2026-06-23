import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerInvoiceService } from '../../../services/customer-invoice.service';
import { CompanyService } from '../../../services/company.service';
import { CustomerService } from '../../../services/customer.service';
import { CustomerPricingService } from '../../../services/customer-pricing.service';
import { CustomerInvoiceDto } from '../../../models/customer-invoice.models';
import { CompanyDto } from '../../../models/company.models';
import { CustomerDto } from '../../../models/customer.models';
import { numberToWords } from '../../../shared/number-to-words.util';

@Component({
  selector: 'app-commercial-invoice-print',
  standalone: false,
  templateUrl: './commercial-invoice-print.component.html',
  styleUrl: './commercial-invoice-print.component.scss'
})
export class CommercialInvoicePrintComponent implements OnInit {
  get logoSrc(): string { return this.companySvc.logoUrl(); }
  loading = false;
  invoice: CustomerInvoiceDto | null = null;
  company: CompanyDto | null = null;
  buyer: CustomerDto | null = null;

  /** productId → buyer's own article code (from the customer's pricing/code list). */
  buyerCodes: Record<number, string> = {};

  constructor(
    private svc: CustomerInvoiceService,
    private companySvc: CompanyService,
    private customerSvc: CustomerService,
    private pricingSvc: CustomerPricingService,
    private route: ActivatedRoute,
    private router: Router,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loading = true;
    this.svc.getById(id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          this.invoice = res.data;
          this.customerSvc.getById(res.data.customerId).subscribe({
            next: (cr) => this.zone.run(() => {
              if (cr.success && cr.data) this.buyer = cr.data;
              this.tryComplete();
            })
          });
          // Buyer's own article codes for this customer's products (export-doc requirement).
          this.pricingSvc.getForCustomer(res.data.customerId, true).subscribe({
            next: (pr) => this.zone.run(() => {
              if (pr.success && pr.data) {
                for (const row of pr.data) {
                  if (row.buyerProductCode) this.buyerCodes[row.productId] = row.buyerProductCode;
                }
              }
              this.cdr.detectChanges();
            })
          });
        }
        this.tryComplete();
      })
    });
    this.companySvc.getCompany().subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.company = res.data; this.tryComplete(); })
    });
  }
  private tryComplete(): void {
    if (this.invoice && this.company) { this.loading = false; this.cdr.detectChanges(); }
  }

  back(): void { this.router.navigate(['/customer-invoices']); }
  print(): void { window.print(); }

  formatCurrency(amount: number, code = 'USD'): string {
    try {
      return new Intl.NumberFormat('en-US', { style: 'currency', currency: code, maximumFractionDigits: 2 }).format(amount || 0);
    } catch {
      return `${(amount || 0).toLocaleString('en-US', { maximumFractionDigits: 2 })} ${code}`;
    }
  }
  amountInWords(amount: number, currencyName: string): string {
    return numberToWords(amount) + ' ' + currencyName + ' Only';
  }

  countryOfDestination(): string {
    return this.invoice?.countryOfDestination?.trim()
        || this.buyer?.country
        || '';
  }

  totalQty(): number {
    return (this.invoice?.lines ?? []).reduce((s, l) => s + (l.quantity || 0), 0);
  }

  /** The buyer's own code for a product line, if mapped for this customer. */
  buyerCodeFor(productId: number): string | null {
    return this.buyerCodes[productId] ?? null;
  }
}
