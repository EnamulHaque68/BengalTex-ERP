import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { BudgetDto, BudgetDetailDto, BudgetLineInput, BudgetVarianceReportDto } from '../models/budget.models';

/** Phase A7a — annual budgets + Budget-vs-Actual variance. */
@Injectable({ providedIn: 'root' })
export class BudgetService {
  private readonly base = `${environment.apiBaseUrl}/api/budgets`;

  constructor(private http: HttpClient) {}

  getAll(financialYearId?: number): Observable<ApiResponse<BudgetDto[]>> {
    let params = new HttpParams();
    if (financialYearId) params = params.set('financialYearId', financialYearId.toString());
    return this.http.get<ApiResponse<BudgetDto[]>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<BudgetDetailDto>> {
    return this.http.get<ApiResponse<BudgetDetailDto>>(`${this.base}/${id}`);
  }

  variance(id: number, fromMonth: number, toMonth: number, costCenterId?: number): Observable<ApiResponse<BudgetVarianceReportDto>> {
    let params = new HttpParams().set('fromMonth', fromMonth).set('toMonth', toMonth);
    if (costCenterId) params = params.set('costCenterId', costCenterId.toString());
    return this.http.get<ApiResponse<BudgetVarianceReportDto>>(`${this.base}/${id}/variance`, { params });
  }

  create(body: { financialYearId: number; name: string; notes: string | null }): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, body);
  }

  setLines(id: number, lines: BudgetLineInput[]): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/${id}/lines`, { lines });
  }

  approve(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/approve`, {});
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
