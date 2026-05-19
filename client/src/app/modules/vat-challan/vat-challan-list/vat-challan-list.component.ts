import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { VatChallanService } from '../../../services/vat-challan.service';
import { CustomerService } from '../../../services/customer.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { VatChallanDto, VatChallanListItemDto } from '../../../models/vat-challan.models';
import { CustomerListItemDto } from '../../../models/customer.models';

@Component({
  selector: 'app-vat-challan-list',
  standalone: false,
  templateUrl: './vat-challan-list.component.html',
  styleUrl: './vat-challan-list.component.scss'
})
export class VatChallanListComponent implements OnInit {

  challans: VatChallanListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterCustomerId: number | null = null;
  fromDate: string | null = null;
  toDate: string | null = null;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  customers: CustomerListItemDto[] = [];

  viewDialogVisible = false;
  viewingChallan: VatChallanDto | null = null;

  constructor(
    private challanService: VatChallanService,
    private customerService: CustomerService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadCustomers();
    this.load();
  }

  private loadCustomers(): void {
    this.customerService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.customers = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.challanService.getAll(
      this.parameters,
      this.filterCustomerId ?? undefined,
      this.fromDate ?? undefined,
      this.toDate ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.challans = res.data.items;
            this.totalCount = res.data.totalCount;
          }
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); });
      }
    });
  }

  onSearchChange(value: string): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.parameters.search = value;
      this.parameters.page = 1;
      this.load();
    }, 400);
  }

  onPageChange(event: any): void {
    this.parameters.page = Math.floor(event.first / event.rows) + 1;
    this.parameters.pageSize = event.rows;
    this.load();
  }

  openView(c: VatChallanListItemDto): void {
    this.viewingChallan = null;
    this.viewDialogVisible = true;
    this.challanService.getById(c.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.viewingChallan = res.data;
          this.cdr.detectChanges();
        });
      }
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency', currency: 'BDT', maximumFractionDigits: 2
    }).format(amount || 0);
  }

  print(): void {
    window.print();
  }
}
