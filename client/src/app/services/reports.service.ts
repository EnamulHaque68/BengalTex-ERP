import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  ApAgeingReportDto,
  ArAgeingReportDto,
  DashboardKpisDto,
  MarginReportDto,
  SalesSummaryReportDto,
  StockSummaryReportDto,
  VatSummaryReportDto
} from '../models/reports.models';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly base = `${environment.apiBaseUrl}/api/reports`;

  constructor(private http: HttpClient) {}

  getStockSummary(itemType?: string, warehouseId?: number): Observable<ApiResponse<StockSummaryReportDto>> {
    let params = new HttpParams();
    if (itemType) params = params.set('itemType', itemType);
    if (warehouseId) params = params.set('warehouseId', warehouseId.toString());
    return this.http.get<ApiResponse<StockSummaryReportDto>>(`${this.base}/stock-summary`, { params });
  }

  getArAgeing(asOfDate?: string, customerId?: number): Observable<ApiResponse<ArAgeingReportDto>> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    if (customerId) params = params.set('customerId', customerId.toString());
    return this.http.get<ApiResponse<ArAgeingReportDto>>(`${this.base}/ar-ageing`, { params });
  }

  getApAgeing(asOfDate?: string, supplierId?: number): Observable<ApiResponse<ApAgeingReportDto>> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    if (supplierId) params = params.set('supplierId', supplierId.toString());
    return this.http.get<ApiResponse<ApAgeingReportDto>>(`${this.base}/ap-ageing`, { params });
  }

  getSalesSummary(fromDate?: string, toDate?: string, customerId?: number): Observable<ApiResponse<SalesSummaryReportDto>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    if (customerId) params = params.set('customerId', customerId.toString());
    return this.http.get<ApiResponse<SalesSummaryReportDto>>(`${this.base}/sales-summary`, { params });
  }

  getDashboardKpis(): Observable<ApiResponse<DashboardKpisDto>> {
    return this.http.get<ApiResponse<DashboardKpisDto>>(`${this.base}/dashboard-kpis`);
  }

  getVatSummary(fromDate?: string, toDate?: string): Observable<ApiResponse<VatSummaryReportDto>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<VatSummaryReportDto>>(`${this.base}/vat-summary`, { params });
  }

  getMargin(fromDate?: string, toDate?: string, customerId?: number): Observable<ApiResponse<MarginReportDto>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    if (customerId) params = params.set('customerId', customerId.toString());
    return this.http.get<ApiResponse<MarginReportDto>>(`${this.base}/margin`, { params });
  }
}
