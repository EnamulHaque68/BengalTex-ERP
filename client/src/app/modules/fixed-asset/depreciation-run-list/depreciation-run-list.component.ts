import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FixedAssetService } from '../../../services/fixed-asset.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { AssetDepreciationRunDto } from '../../../models/fixed-asset.models';

@Component({
  selector: 'app-depreciation-run-list',
  standalone: false,
  templateUrl: './depreciation-run-list.component.html',
  styleUrl: './depreciation-run-list.component.scss'
})
export class DepreciationRunListComponent implements OnInit {
  runs: AssetDepreciationRunDto[] = [];
  loading = false;
  totalCount = 0;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };

  viewVisible = false;
  viewing: AssetDepreciationRunDto | null = null;
  viewLoading = false;

  constructor(
    private svc: FixedAssetService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.svc.getRuns(this.parameters).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.runs = res.data.items;
          this.totalCount = res.data.totalCount;
        }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onPageChange(event: any): void {
    this.parameters.page = Math.floor(event.first / event.rows) + 1;
    this.parameters.pageSize = event.rows;
    this.load();
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }

  openView(r: AssetDepreciationRunDto): void {
    this.viewLoading = true;
    this.viewVisible = true;
    this.svc.getRunById(r.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.viewLoading = false;
        if (res.success && res.data) this.viewing = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.viewLoading = false; this.cdr.detectChanges(); })
    });
  }
}
