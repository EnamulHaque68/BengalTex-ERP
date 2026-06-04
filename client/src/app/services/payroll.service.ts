import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  PayslipDto, GeneratePayrollRequest, UpdatePayslipRequest, PayslipPrintDto
} from '../models/payroll.models';

@Injectable({ providedIn: 'root' })
export class PayrollService {
  private readonly base = `${environment.apiBaseUrl}/api/payroll`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    year?: number,
    month?: number,
    employeeId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<PayslipDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (year) params = params.set('year', year.toString());
    if (month) params = params.set('month', month.toString());
    if (employeeId) params = params.set('employeeId', employeeId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<PayslipDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<PayslipDto>> {
    return this.http.get<ApiResponse<PayslipDto>>(`${this.base}/${id}`);
  }

  getForPrint(id: number): Observable<ApiResponse<PayslipPrintDto>> {
    return this.http.get<ApiResponse<PayslipPrintDto>>(`${this.base}/${id}/print-data`);
  }

  generate(data: GeneratePayrollRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/generate`, data);
  }

  update(id: number, data: UpdatePayslipRequest): Observable<ApiResponse<PayslipDto>> {
    return this.http.put<ApiResponse<PayslipDto>>(`${this.base}/${id}`, data);
  }

  markPaid(id: number): Observable<ApiResponse<PayslipDto>> {
    return this.http.post<ApiResponse<PayslipDto>>(`${this.base}/${id}/mark-paid`, {});
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
