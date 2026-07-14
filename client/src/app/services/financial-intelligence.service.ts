import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { FinancialKpisDto, ArApAgingDto, ProfitTrendDto } from '../models/financial-intelligence.models';

/** Phase A8 — financial ratios, AR/AP aging, and the P&L trend. */
@Injectable({ providedIn: 'root' })
export class FinancialIntelligenceService {
  private readonly base = `${environment.apiBaseUrl}/api/financial-intelligence`;

  constructor(private http: HttpClient) {}

  kpis(asOfDate?: string, fromDate?: string, toDate?: string): Observable<ApiResponse<FinancialKpisDto>> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<FinancialKpisDto>>(`${this.base}/kpis`, { params });
  }

  aging(asOfDate?: string): Observable<ApiResponse<ArApAgingDto>> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    return this.http.get<ApiResponse<ArApAgingDto>>(`${this.base}/ar-ap-aging`, { params });
  }

  profitTrend(months = 12): Observable<ApiResponse<ProfitTrendDto>> {
    const params = new HttpParams().set('months', months.toString());
    return this.http.get<ApiResponse<ProfitTrendDto>>(`${this.base}/profit-trend`, { params });
  }
}
