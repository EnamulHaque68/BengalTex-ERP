import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { DashboardSnapshotDto, ExpenseBreakdownItemDto, ProductionOverviewDto } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly base = `${environment.apiBaseUrl}/api/dashboard`;
  constructor(private http: HttpClient) {}

  getSnapshot(): Observable<ApiResponse<DashboardSnapshotDto>> {
    return this.http.get<ApiResponse<DashboardSnapshotDto>>(`${this.base}/snapshot`);
  }

  /** Expense breakdown for a date range (period filter). */
  expenseBreakdown(fromDate: string, toDate: string): Observable<ApiResponse<ExpenseBreakdownItemDto[]>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<ExpenseBreakdownItemDto[]>>(`${this.base}/expense-breakdown`, { params });
  }

  /** Production target/produced/achievement for a date range (period filter). */
  productionOverview(fromDate: string, toDate: string): Observable<ApiResponse<ProductionOverviewDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<ProductionOverviewDto>>(`${this.base}/production-overview`, { params });
  }
}
