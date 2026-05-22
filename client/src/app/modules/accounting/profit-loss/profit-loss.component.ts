import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService } from '../../../services/accounting.service';
import { ProfitAndLossDto } from '../../../models/accounting.models';

@Component({
  selector: 'app-profit-loss',
  standalone: false,
  templateUrl: './profit-loss.component.html',
  styleUrl: './profit-loss.component.scss'
})
export class ProfitLossComponent implements OnInit {
  fromDate: string;
  toDate: string;
  data: ProfitAndLossDto | null = null;
  loading = false;

  constructor(private svc: AccountingService, private zone: NgZone, private cdr: ChangeDetectorRef) {
    const now = new Date();
    this.fromDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
    this.toDate = now.toISOString().slice(0, 10);
  }

  ngOnInit(): void { this.run(); }

  run(): void {
    this.loading = true;
    this.svc.profitAndLoss(this.fromDate, this.toDate).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success) this.data = res.data ?? null; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }
}
