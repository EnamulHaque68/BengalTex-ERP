import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerInvoiceService } from '../../../services/customer-invoice.service';
import { CompanyService } from '../../../services/company.service';
import { CustomerService } from '../../../services/customer.service';
import { CustomerInvoiceDto } from '../../../models/customer-invoice.models';
import { CompanyDto } from '../../../models/company.models';
import { CustomerDto } from '../../../models/customer.models';

@Component({
  selector: 'app-packing-list-print',
  standalone: false,
  templateUrl: './packing-list-print.component.html',
  styleUrl: './packing-list-print.component.scss'
})
export class PackingListPrintComponent implements OnInit {
  loading = false;
  invoice: CustomerInvoiceDto | null = null;
  company: CompanyDto | null = null;
  buyer: CustomerDto | null = null;

  constructor(
    private svc: CustomerInvoiceService,
    private companySvc: CompanyService,
    private customerSvc: CustomerService,
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

  countryOfDestination(): string {
    return this.invoice?.countryOfDestination?.trim()
        || this.buyer?.country
        || '';
  }

  totalQty(): number {
    return (this.invoice?.lines ?? []).reduce((s, l) => s + (l.quantity || 0), 0);
  }
}
