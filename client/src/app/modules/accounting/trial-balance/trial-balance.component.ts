import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService } from '../../../services/accounting.service';
import { TrialBalanceDto } from '../../../models/accounting.models';

@Component({
  selector: 'app-trial-balance',
  standalone: false,
  templateUrl: './trial-balance.component.html',
  styleUrl: './trial-balance.component.scss'
})
export class TrialBalanceComponent implements OnInit {
  asOfDate: string = new Date().toISOString().slice(0, 10);
  data: TrialBalanceDto | null = null;
  loading = false;

  constructor(private svc: AccountingService, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void { this.run(); }

  run(): void {
    this.loading = true;
    this.svc.trialBalance(this.asOfDate || undefined).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success) this.data = res.data ?? null; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  typeClass(t: string): string { return t.toLowerCase(); }
}
