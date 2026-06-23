import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { GoodsReceiptService } from '../../../services/goods-receipt.service';
import { CompanyService } from '../../../services/company.service';
import { GoodsReceiptDto } from '../../../models/goods-receipt.models';
import { CompanyDto } from '../../../models/company.models';

@Component({
  selector: 'app-goods-receipt-print',
  standalone: false,
  templateUrl: './goods-receipt-print.component.html',
  styleUrl: './goods-receipt-print.component.scss'
})
export class GoodsReceiptPrintComponent implements OnInit {
  get logoSrc(): string { return this.companySvc.logoUrl(); }
  loading = false;
  grn: GoodsReceiptDto | null = null;
  company: CompanyDto | null = null;

  constructor(
    private svc: GoodsReceiptService,
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
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.grn = res.data; this.tryComplete(); })
    });
    this.companySvc.getCompany().subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.company = res.data; this.tryComplete(); })
    });
  }
  private tryComplete(): void { if (this.grn && this.company) { this.loading = false; this.cdr.detectChanges(); } }

  back(): void { this.router.navigate(['/goods-receipts']); }
  print(): void { window.print(); }

  totalQty(): number {
    if (!this.grn) return 0;
    return this.grn.lines.reduce((s, l) => s + (l.receivedQuantity || 0), 0);
  }
}
