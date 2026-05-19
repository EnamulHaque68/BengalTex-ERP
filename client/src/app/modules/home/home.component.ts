import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { ReportsService } from '../../services/reports.service';
import { DashboardKpisDto } from '../../models/reports.models';

@Component({
  selector: 'app-home',
  standalone: false,
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  userName = '';
  userRoles: string[] = [];

  kpis: DashboardKpisDto | null = null;
  kpisLoading = false;
  kpisError = '';

  constructor(
    private authService: AuthService,
    private reportsService: ReportsService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    this.userName = user?.fullName || user?.userName || 'User';
    this.userRoles = user?.roles ?? [];
    this.loadKpis();
  }

  private loadKpis(): void {
    this.kpisLoading = true;
    this.reportsService.getDashboardKpis().subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.kpisLoading = false;
          if (res.success && res.data) {
            this.kpis = res.data;
          } else {
            // soft-fail: don't show banner if user lacks Dashboard.ViewOwner — just hide tiles
            this.kpis = null;
          }
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => {
          this.kpisLoading = false;
          this.kpis = null;
          this.cdr.detectChanges();
        });
      }
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency', currency: 'BDT', maximumFractionDigits: 0
    }).format(amount || 0);
  }

  logout(): void {
    this.authService.logout();
  }
}
