import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { VatChallanService } from '../../../services/vat-challan.service';
import { CustomerInvoiceService } from '../../../services/customer-invoice.service';
import { CompanyService } from '../../../services/company.service';
import { VatChallanDto } from '../../../models/vat-challan.models';
import { CustomerInvoiceDto } from '../../../models/customer-invoice.models';
import { CompanyDto } from '../../../models/company.models';
import { numberToWords } from '../../../shared/number-to-words.util';

@Component({
  selector: 'app-vat-challan-print',
  standalone: false,
  templateUrl: './vat-challan-print.component.html',
  styleUrl: './vat-challan-print.component.scss'
})
export class VatChallanPrintComponent implements OnInit {
  loading = false;
  challan: VatChallanDto | null = null;
  invoice: CustomerInvoiceDto | null = null;
  company: CompanyDto | null = null;

  constructor(
    private svc: VatChallanService,
    private invoiceSvc: CustomerInvoiceService,
    private companySvc: CompanyService,
    private route: ActivatedRoute,
    private router: Router,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loading = true;
    this.companySvc.getCompany().subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.company = res.data; this.tryComplete(); })
    });
    this.svc.getById(id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          this.challan = res.data;
          // Fetch full invoice for line detail (Mushok requires line-level breakdown)
          this.invoiceSvc.getById(res.data.customerInvoiceId).subscribe({
            next: (r2) => this.zone.run(() => { if (r2.success && r2.data) this.invoice = r2.data; this.tryComplete(); })
          });
        }
      })
    });
  }
  private tryComplete(): void { if (this.challan && this.invoice && this.company) { this.loading = false; this.cdr.detectChanges(); } }

  back(): void { this.router.navigate(['/vat-challans']); }
  print(): void { window.print(); }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }
  amountInWords(amount: number): string { return numberToWords(amount) + ' Taka Only'; }
}
