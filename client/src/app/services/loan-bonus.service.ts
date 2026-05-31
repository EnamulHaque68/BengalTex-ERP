import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  EmployeeLoanDto, CreateEmployeeLoanRequest, UpdateEmployeeLoanRequest, EmployeeLoanStatus,
  FestivalBonusDto, BulkCreateFestivalBonusRequest, UpdateFestivalBonusRequest,
  FestivalBonusType, FestivalBonusStatus
} from '../models/loan-bonus.models';

@Injectable({ providedIn: 'root' })
export class LoanBonusService {
  private readonly loans = `${environment.apiBaseUrl}/api/employee-loans`;
  private readonly bonuses = `${environment.apiBaseUrl}/api/festival-bonuses`;

  constructor(private http: HttpClient) {}

  // ── Loans ──
  getLoans(parameters: PagedQueryParameters, status?: EmployeeLoanStatus | null, employeeId?: number)
    : Observable<ApiResponse<PagedResult<EmployeeLoanDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (status) params = params.set('status', status);
    if (employeeId) params = params.set('employeeId', employeeId.toString());
    return this.http.get<ApiResponse<PagedResult<EmployeeLoanDto>>>(this.loans, { params });
  }
  createLoan(data: CreateEmployeeLoanRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.loans, data);
  }
  updateLoan(id: number, data: UpdateEmployeeLoanRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.loans}/${id}`, data);
  }
  closeLoan(id: number, cancel = false): Observable<ApiResponse<null>> {
    const params = new HttpParams().set('cancel', cancel.toString());
    return this.http.post<ApiResponse<null>>(`${this.loans}/${id}/close`, {}, { params });
  }

  // ── Festival Bonuses ──
  getBonuses(parameters: PagedQueryParameters, year?: number, bonusType?: FestivalBonusType | null,
             status?: FestivalBonusStatus | null, employeeId?: number)
    : Observable<ApiResponse<PagedResult<FestivalBonusDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (year) params = params.set('year', year.toString());
    if (bonusType) params = params.set('bonusType', bonusType);
    if (status) params = params.set('status', status);
    if (employeeId) params = params.set('employeeId', employeeId.toString());
    return this.http.get<ApiResponse<PagedResult<FestivalBonusDto>>>(this.bonuses, { params });
  }
  bulkCreateBonus(data: BulkCreateFestivalBonusRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.bonuses}/bulk-create`, data);
  }
  updateBonus(id: number, data: UpdateFestivalBonusRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.bonuses}/${id}`, data);
  }
  pay(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.bonuses}/${id}/pay`, {});
  }
  deleteBonus(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.bonuses}/${id}`);
  }
}
