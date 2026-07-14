import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentService } from '../../../services/payment.service';
import { CompanyService } from '../../../services/company.service';
import { WithholdingCertificateDto } from '../../../models/payment.models';
import { numberToWords } from '../../../shared/number-to-words.util';
import { apiErrorMessage } from '../../../shared/utils/http-error.util';

/**
 * Phase A5b — printable AIT/VDS withholding certificate issued to a supplier, evidencing the tax
 * the company (as withholding agent) deducted at source and holds for remittance to the NBR.
 */
@Component({
  selector: 'app-withholding-certificate',
  standalone: false,
  templateUrl: './withholding-certificate.component.html',
  styleUrl: './withholding-certificate.component.scss'
})
export class WithholdingCertificateComponent implements OnInit {
  get logoSrc(): string { return this.companySvc.logoUrl(); }
  loading = false;
  error = '';
  cert: WithholdingCertificateDto | null = null;

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
    this.svc.withholdingCertificate(id).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.cert = res.data;
        else this.error = res.message || 'Certificate not available.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loading = false;
        this.error = apiErrorMessage(err, 'Certificate not available.');
        this.cdr.detectChanges();
      })
    });
  }

  back(): void { this.router.navigate(['/payments']); }
  print(): void { window.print(); }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }
  amountInWords(amount: number): string { return numberToWords(amount) + ' Taka Only'; }
}
