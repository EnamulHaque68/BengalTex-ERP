import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { QuotationService } from '../../../services/quotation.service';
import { CompanyService } from '../../../services/company.service';
import { QuotationDto } from '../../../models/quotation.models';
import { CompanyDto } from '../../../models/company.models';
import { numberToWords } from '../../../shared/number-to-words.util';

@Component({
  selector: 'app-quotation-print',
  standalone: false,
  templateUrl: './quotation-print.component.html',
  styleUrl: './quotation-print.component.scss'
})
export class QuotationPrintComponent implements OnInit {
  loading = false;
  quote: QuotationDto | null = null;
  company: CompanyDto | null = null;

  constructor(
    private svc: QuotationService,
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
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.quote = res.data; this.tryComplete(); })
    });
    this.companySvc.getCompany().subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.company = res.data; this.tryComplete(); })
    });
  }
  private tryComplete(): void { if (this.quote && this.company) { this.loading = false; this.cdr.detectChanges(); } }

  back(): void { this.router.navigate(['/quotations']); }
  print(): void { window.print(); }

  formatCurrency(amount: number, code = 'BDT'): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: code, maximumFractionDigits: 2 }).format(amount || 0);
  }
  amountInWords(amount: number, currencyName = 'Taka'): string {
    return numberToWords(amount) + ' ' + currencyName + ' Only';
  }
}
