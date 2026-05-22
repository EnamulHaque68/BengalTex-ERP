import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AccountingService } from '../../../services/accounting.service';
import { BalanceSheetDto } from '../../../models/accounting.models';

@Component({
  selector: 'app-balance-sheet',
  standalone: false,
  templateUrl: './balance-sheet.component.html',
  styleUrl: './balance-sheet.component.scss'
})
export class BalanceSheetComponent implements OnInit {
  asOfDate: string = new Date().toISOString().slice(0, 10);
  data: BalanceSheetDto | null = null;
  loading = false;

  constructor(private svc: AccountingService, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void { this.run(); }

  run(): void {
    this.loading = true;
    this.svc.balanceSheet(this.asOfDate || undefined).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success) this.data = res.data ?? null; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }
}
