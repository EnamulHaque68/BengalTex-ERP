import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DeliveryNoteService } from '../../../services/delivery-note.service';
import { CompanyService } from '../../../services/company.service';
import { DeliveryNoteDto } from '../../../models/delivery-note.models';
import { CompanyDto } from '../../../models/company.models';

@Component({
  selector: 'app-delivery-note-print',
  standalone: false,
  templateUrl: './delivery-note-print.component.html',
  styleUrl: './delivery-note-print.component.scss'
})
export class DeliveryNotePrintComponent implements OnInit {
  get logoSrc(): string { return this.companySvc.logoUrl(); }
  loading = false;
  dn: DeliveryNoteDto | null = null;
  company: CompanyDto | null = null;

  constructor(
    private svc: DeliveryNoteService,
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
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.dn = res.data; this.tryComplete(); })
    });
    this.companySvc.getCompany().subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.company = res.data; this.tryComplete(); })
    });
  }
  private tryComplete(): void { if (this.dn && this.company) { this.loading = false; this.cdr.detectChanges(); } }

  back(): void { this.router.navigate(['/delivery-notes']); }
  print(): void { window.print(); }

  totalQty(): number {
    if (!this.dn) return 0;
    return this.dn.lines.reduce((s, l) => s + (l.dispatchedQuantity || 0), 0);
  }
}
