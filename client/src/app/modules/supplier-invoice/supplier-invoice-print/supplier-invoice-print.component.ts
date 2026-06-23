import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SupplierInvoiceService } from '../../../services/supplier-invoice.service';
import { CompanyService } from '../../../services/company.service';
import { SupplierInvoiceDto } from '../../../models/supplier-invoice.models';
import { CompanyDto } from '../../../models/company.models';
import { numberToWords } from '../../../shared/number-to-words.util';

@Component({
  selector: 'app-supplier-invoice-print',
  standalone: false,
  templateUrl: './supplier-invoice-print.component.html',
  styleUrl: './supplier-invoice-print.component.scss'
})
export class SupplierInvoicePrintComponent implements OnInit {
  get logoSrc(): string { return this.companySvc.logoUrl(); }
  loading = false;
  inv: SupplierInvoiceDto | null = null;
  company: CompanyDto | null = null;

  constructor(
    private svc: SupplierInvoiceService,
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
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.inv = res.data; this.tryComplete(); })
    });
    this.companySvc.getCompany().subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.company = res.data; this.tryComplete(); })
    });
  }
  private tryComplete(): void { if (this.inv && this.company) { this.loading = false; this.cdr.detectChanges(); } }

  back(): void { this.router.navigate(['/supplier-invoices']); }
  print(): void { window.print(); }

  formatCurrency(amount: number, code = 'BDT'): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: code, maximumFractionDigits: 2 }).format(amount || 0);
  }
  amountInWords(amount: number, currencyName = 'Taka'): string {
    return numberToWords(amount) + ' ' + currencyName + ' Only';
  }
  vatRateText(rate: number): string { return (rate * 100).toFixed(0) + '%'; }
}
