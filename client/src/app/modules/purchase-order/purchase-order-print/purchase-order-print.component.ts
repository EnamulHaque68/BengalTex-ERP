import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PurchaseOrderService } from '../../../services/purchase-order.service';
import { CompanyService } from '../../../services/company.service';
import { PurchaseOrderDto } from '../../../models/purchase-order.models';
import { CompanyDto } from '../../../models/company.models';
import { numberToWords } from '../../../shared/number-to-words.util';

@Component({
  selector: 'app-purchase-order-print',
  standalone: false,
  templateUrl: './purchase-order-print.component.html',
  styleUrl: './purchase-order-print.component.scss'
})
export class PurchaseOrderPrintComponent implements OnInit {
  get logoSrc(): string { return this.companySvc.logoUrl(); }
  loading = false;
  po: PurchaseOrderDto | null = null;
  company: CompanyDto | null = null;

  constructor(
    private svc: PurchaseOrderService,
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
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.po = res.data; this.tryComplete(); })
    });
    this.companySvc.getCompany().subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.company = res.data; this.tryComplete(); })
    });
  }
  private tryComplete(): void { if (this.po && this.company) { this.loading = false; this.cdr.detectChanges(); } }

  back(): void { this.router.navigate(['/purchase-orders']); }
  print(): void { window.print(); }

  formatCurrency(amount: number, code = 'BDT'): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: code, maximumFractionDigits: 2 }).format(amount || 0);
  }
  amountInWords(amount: number, currencyName = 'Taka'): string {
    return numberToWords(amount) + ' ' + currencyName + ' Only';
  }
}
