import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { WastageService } from '../../../services/wastage.service';
import { WastageSummaryDto } from '../../../models/wastage.models';

@Component({
  selector: 'app-wastage-summary',
  standalone: false,
  templateUrl: './wastage-summary.component.html',
  styleUrl: './wastage-summary.component.scss'
})
export class WastageSummaryComponent implements OnInit {
  fromDate: string;
  toDate: string;
  data: WastageSummaryDto | null = null;
  loading = false;

  constructor(private svc: WastageService, private zone: NgZone, private cdr: ChangeDetectorRef) {
    const now = new Date();
    this.fromDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
    this.toDate = now.toISOString().slice(0, 10);
  }

  ngOnInit(): void { this.run(); }

  run(): void {
    this.loading = true;
    this.svc.summary(this.fromDate, this.toDate).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success) this.data = res.data ?? null; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  pct(cost: number): number {
    if (!this.data || this.data.totalCost === 0) return 0;
    return Math.round((cost / this.data.totalCost) * 100);
  }
}
