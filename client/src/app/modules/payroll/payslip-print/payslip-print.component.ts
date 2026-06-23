import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PayrollService } from '../../../services/payroll.service';
import { CompanyService } from '../../../services/company.service';
import { PayslipPrintDto } from '../../../models/payroll.models';
import { numberToWords } from '../../../shared/number-to-words.util';

@Component({
  selector: 'app-payslip-print',
  standalone: false,
  templateUrl: './payslip-print.component.html',
  styleUrl: './payslip-print.component.scss'
})
export class PayslipPrintComponent implements OnInit {
  loading = false;
  slip: PayslipPrintDto | null = null;

  constructor(
    private svc: PayrollService,
    private companySvc: CompanyService,
    private route: ActivatedRoute,
    private router: Router,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  get logoSrc(): string { return this.companySvc.logoUrl(); }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loading = true;
    this.svc.getForPrint(id).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.slip = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  back(): void { this.router.navigate(['/payroll']); }
  print(): void { window.print(); }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }

  amountInWords(amount: number): string {
    return numberToWords(amount) + ' Taka Only';
  }
}
