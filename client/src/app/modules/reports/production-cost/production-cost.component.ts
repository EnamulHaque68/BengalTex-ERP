import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ReportsService } from '../../../services/reports.service';
import { ProductionCostReportDto } from '../../../models/reports.models';
import { ProductListItemDto } from '../../../models/product.models';
import { ProductService } from '../../../services/product.service';

@Component({
  selector: 'app-production-cost',
  standalone: false,
  templateUrl: './production-cost.component.html',
  styleUrl: './production-cost.component.scss'
})
export class ProductionCostComponent implements OnInit {
  report: ProductionCostReportDto | null = null;
  loading = false;
  error = '';

  fromDate = '';
  toDate = '';
  filterProductId: number | null = null;
  products: ProductListItemDto[] = [];

  constructor(
    private reportsService: ReportsService,
    private productService: ProductService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const today = new Date();
    const thirtyAgo = new Date(today);
    thirtyAgo.setDate(today.getDate() - 30);
    this.toDate = today.toISOString().slice(0, 10);
    this.fromDate = thirtyAgo.toISOString().slice(0, 10);
    this.loadProducts();
    this.load();
  }

  private loadProducts(): void {
    this.productService.getAll({ page: 1, pageSize: 1000, search: '' }).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.products = res.data.items;
        this.cdr.detectChanges();
      })
    });
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();
    this.reportsService.getProductionCost(
      this.fromDate || undefined,
      this.toDate || undefined,
      this.filterProductId ?? undefined
    ).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.report = res.data;
        else { this.report = null; this.error = res.message || 'Failed to load report.'; }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loading = false;
        this.error = err?.error?.message || 'Failed to load report.';
        this.cdr.detectChanges();
      })
    });
  }

  money(n: number | null | undefined): string {
    return new Intl.NumberFormat('en-BD', { maximumFractionDigits: 2 }).format(n || 0);
  }

  num(n: number | null | undefined): string {
    return new Intl.NumberFormat('en-BD', { maximumFractionDigits: 4 }).format(n || 0);
  }
}
