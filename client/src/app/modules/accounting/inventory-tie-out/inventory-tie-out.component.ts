import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService, InventoryGlTieOutDto } from '../../../services/accounting.service';

/**
 * Phase A2 (D7) — Inventory ↔ GL tie-out. Reconciles perpetual stock valuation (Σ qty × WAC)
 * against the GL inventory accounts (RM/FG/WIP) and lists the PO-wise received-not-billed
 * schedule behind the GR/IR balance. The monthly audit proof that books = stock.
 */
@Component({
  selector: 'app-inventory-tie-out',
  standalone: false,
  templateUrl: './inventory-tie-out.component.html',
  styleUrl: './inventory-tie-out.component.scss'
})
export class InventoryTieOutComponent implements OnInit {
  data: InventoryGlTieOutDto | null = null;
  loading = false;
  asOfDate = '';

  constructor(private svc: AccountingService, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.asOfDate = new Date().toISOString().slice(0, 10);
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.inventoryGlTieOut(this.asOfDate).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.data = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  get allMatch(): boolean { return !!this.data && this.data.rows.every(r => r.matches); }

  formatMoney(v: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(v || 0);
  }
}
