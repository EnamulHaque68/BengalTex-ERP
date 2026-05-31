import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  LeaveTypeDto, SaveLeaveTypeRequest,
  HolidayDto, SaveHolidayRequest,
  LeaveBalanceDto,
  LeaveApplicationDto, LeaveApplicationListItemDto, CreateLeaveApplicationRequest,
  LeaveApplicationStatus
} from '../models/leaves.models';

@Injectable({ providedIn: 'root' })
export class LeavesService {
  private readonly leaveTypes = `${environment.apiBaseUrl}/api/leave-types`;
  private readonly holidays = `${environment.apiBaseUrl}/api/holidays`;
  private readonly balances = `${environment.apiBaseUrl}/api/leave-balances`;
  private readonly leaves = `${environment.apiBaseUrl}/api/leaves`;

  constructor(private http: HttpClient) {}

  // ── Leave Types ──
  getLeaveTypes(includeInactive = false): Observable<ApiResponse<LeaveTypeDto[]>> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<LeaveTypeDto[]>>(this.leaveTypes, { params });
  }
  createLeaveType(data: SaveLeaveTypeRequest): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(this.leaveTypes, data); }
  updateLeaveType(id: number, data: SaveLeaveTypeRequest): Observable<ApiResponse<number>> { return this.http.put<ApiResponse<number>>(`${this.leaveTypes}/${id}`, data); }
  deleteLeaveType(id: number): Observable<ApiResponse<null>> { return this.http.delete<ApiResponse<null>>(`${this.leaveTypes}/${id}`); }

  // ── Holidays ──
  getHolidays(year?: number, includeInactive = false): Observable<ApiResponse<HolidayDto[]>> {
    let params = new HttpParams().set('includeInactive', includeInactive.toString());
    if (year) params = params.set('year', year.toString());
    return this.http.get<ApiResponse<HolidayDto[]>>(this.holidays, { params });
  }
  createHoliday(data: SaveHolidayRequest): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(this.holidays, data); }
  updateHoliday(id: number, data: SaveHolidayRequest): Observable<ApiResponse<number>> { return this.http.put<ApiResponse<number>>(`${this.holidays}/${id}`, data); }
  deleteHoliday(id: number): Observable<ApiResponse<null>> { return this.http.delete<ApiResponse<null>>(`${this.holidays}/${id}`); }

  // ── Balances ──
  getBalances(year: number, employeeId?: number): Observable<ApiResponse<LeaveBalanceDto[]>> {
    let params = new HttpParams().set('year', year.toString());
    if (employeeId) params = params.set('employeeId', employeeId.toString());
    return this.http.get<ApiResponse<LeaveBalanceDto[]>>(this.balances, { params });
  }
  initializeYear(year: number): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.balances}/initialize/${year}`, {});
  }
  adjustBalance(id: number, entitled: number, taken: number): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.balances}/${id}`, { entitled, taken });
  }

  // ── Leave Applications ──
  getAll(parameters: PagedQueryParameters, status?: LeaveApplicationStatus | null, employeeId?: number,
         leaveTypeId?: number, fromDate?: string, toDate?: string)
    : Observable<ApiResponse<PagedResult<LeaveApplicationListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (status) params = params.set('status', status);
    if (employeeId) params = params.set('employeeId', employeeId.toString());
    if (leaveTypeId) params = params.set('leaveTypeId', leaveTypeId.toString());
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<PagedResult<LeaveApplicationListItemDto>>>(this.leaves, { params });
  }
  getById(id: number): Observable<ApiResponse<LeaveApplicationDto>> { return this.http.get<ApiResponse<LeaveApplicationDto>>(`${this.leaves}/${id}`); }
  create(data: CreateLeaveApplicationRequest): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(this.leaves, data); }
  approve(id: number): Observable<ApiResponse<null>> { return this.http.post<ApiResponse<null>>(`${this.leaves}/${id}/approve`, {}); }
  reject(id: number, rejectionReason: string | null): Observable<ApiResponse<null>> { return this.http.post<ApiResponse<null>>(`${this.leaves}/${id}/reject`, { rejectionReason }); }
  cancel(id: number): Observable<ApiResponse<null>> { return this.http.post<ApiResponse<null>>(`${this.leaves}/${id}/cancel`, {}); }
}
