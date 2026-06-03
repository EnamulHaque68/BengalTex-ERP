import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  FinalSettlementDto, FinalSettlementPreviewDto,
  CreateFinalSettlementRequest, MarkFinalSettlementPaidRequest
} from '../models/final-settlement.models';

@Injectable({ providedIn: 'root' })
export class FinalSettlementService {
  private readonly base = `${environment.apiBaseUrl}/api/final-settlements`;
  private readonly payrollBase = `${environment.apiBaseUrl}/api/payroll`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    status?: string,
    employeeId?: number
  ): Observable<ApiResponse<PagedResult<FinalSettlementDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (status) params = params.set('status', status);
    if (employeeId) params = params.set('employeeId', employeeId.toString());
    return this.http.get<ApiResponse<PagedResult<FinalSettlementDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<FinalSettlementDto>> {
    return this.http.get<ApiResponse<FinalSettlementDto>>(`${this.base}/${id}`);
  }

  calculate(employeeId: number, lastWorkingDate: string): Observable<ApiResponse<FinalSettlementPreviewDto>> {
    const params = new HttpParams()
      .set('employeeId', employeeId.toString())
      .set('lastWorkingDate', lastWorkingDate);
    return this.http.get<ApiResponse<FinalSettlementPreviewDto>>(`${this.base}/calculate`, { params });
  }

  create(data: CreateFinalSettlementRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  approve(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/approve`, {});
  }

  markPaid(id: number, data: MarkFinalSettlementPaidRequest): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/mark-paid`, data);
  }

  cancel(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/cancel`, {});
  }

  /** Bank-advice CSV — payroll month YYYY-MM. Triggers a browser download. */
  downloadBankAdvice(year: number, month: number): Observable<Blob> {
    const params = new HttpParams().set('year', year.toString()).set('month', month.toString());
    return this.http.get(`${this.payrollBase}/bank-advice`, {
      params, responseType: 'blob'
    });
  }
}
