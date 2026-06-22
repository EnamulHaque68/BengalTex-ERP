import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  AttendanceRecordDto,
  CreateAttendanceRequest,
  UpdateAttendanceRequest,
  SelfCheckInRequest,
  SelfCheckOutRequest,
  MyAttendanceDto,
  TeamAttendanceDto,
  AttendanceRequestDto,
  SubmitAttendanceRequest,
  AttendanceSettingsDto,
  OfficeLocationDto,
  OfficeLocationEmployeeDto,
  UpsertOfficeLocation,
  DailyRegisterDto,
  MonthlySummaryDto,
  AttendanceExceptionsDto
} from '../models/attendance.models';

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly base = `${environment.apiBaseUrl}/api/attendance`;
  private readonly locBase = `${environment.apiBaseUrl}/api/office-locations`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    fromDate?: string,
    toDate?: string,
    employeeId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<AttendanceRecordDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    if (employeeId) params = params.set('employeeId', employeeId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<AttendanceRecordDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<AttendanceRecordDto>> {
    return this.http.get<ApiResponse<AttendanceRecordDto>>(`${this.base}/${id}`);
  }

  create(data: CreateAttendanceRequest): Observable<ApiResponse<AttendanceRecordDto>> {
    return this.http.post<ApiResponse<AttendanceRecordDto>>(this.base, data);
  }

  update(id: number, data: UpdateAttendanceRequest): Observable<ApiResponse<AttendanceRecordDto>> {
    return this.http.put<ApiResponse<AttendanceRecordDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  selfCheckIn(data: SelfCheckInRequest): Observable<ApiResponse<AttendanceRecordDto>> {
    return this.http.post<ApiResponse<AttendanceRecordDto>>(`${this.base}/check-in`, data);
  }

  selfCheckOut(data: SelfCheckOutRequest): Observable<ApiResponse<AttendanceRecordDto>> {
    return this.http.post<ApiResponse<AttendanceRecordDto>>(`${this.base}/check-out`, data);
  }

  breakOut(): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/break-out`, {});
  }

  breakIn(): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/break-in`, {});
  }

  getMyAttendance(): Observable<ApiResponse<MyAttendanceDto>> {
    return this.http.get<ApiResponse<MyAttendanceDto>>(`${this.base}/my-attendance`);
  }

  // ── Supervisor: team view, selfie review, approvals ──
  getTeamAttendance(fromDate?: string, toDate?: string, onlyFlagged = false): Observable<ApiResponse<TeamAttendanceDto>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    if (onlyFlagged) params = params.set('onlyFlagged', 'true');
    return this.http.get<ApiResponse<TeamAttendanceDto>>(`${this.base}/team`, { params });
  }

  approveAttendance(id: number, approve: boolean, rejectionReason?: string): Observable<ApiResponse<AttendanceRecordDto>> {
    return this.http.post<ApiResponse<AttendanceRecordDto>>(`${this.base}/${id}/approve`, { approve, rejectionReason });
  }

  /** Auth'd selfie image — fetched as a blob and turned into an object URL by the component. */
  getSelfieBlob(id: number, which: 'in' | 'out' = 'in'): Observable<Blob> {
    const params = new HttpParams().set('which', which);
    return this.http.get(`${this.base}/${id}/selfie`, { params, responseType: 'blob' });
  }

  // ── Attendance correction requests ──
  submitRequest(data: SubmitAttendanceRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/requests`, data);
  }

  getMyRequests(): Observable<ApiResponse<AttendanceRequestDto[]>> {
    return this.http.get<ApiResponse<AttendanceRequestDto[]>>(`${this.base}/requests/mine`);
  }

  cancelRequest(id: number): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/requests/${id}/cancel`, {});
  }

  getTeamRequests(status = 'Pending'): Observable<ApiResponse<AttendanceRequestDto[]>> {
    const params = new HttpParams().set('status', status);
    return this.http.get<ApiResponse<AttendanceRequestDto[]>>(`${this.base}/requests/team`, { params });
  }

  decideRequest(id: number, approve: boolean, reviewNote?: string): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/requests/${id}/decide`, { approve, reviewNote });
  }

  // ── Settings ──
  getSettings(): Observable<ApiResponse<AttendanceSettingsDto>> {
    return this.http.get<ApiResponse<AttendanceSettingsDto>>(`${this.base}/settings`);
  }
  updateSettings(data: Omit<AttendanceSettingsDto, 'id'>): Observable<ApiResponse<AttendanceSettingsDto>> {
    return this.http.put<ApiResponse<AttendanceSettingsDto>>(`${this.base}/settings`, data);
  }

  // ── Reports ──
  getDailyRegister(date: string): Observable<ApiResponse<DailyRegisterDto>> {
    return this.http.get<ApiResponse<DailyRegisterDto>>(`${this.base}/reports/daily-register`, { params: new HttpParams().set('date', date) });
  }
  getMonthlySummary(year: number, month: number, employeeId?: number): Observable<ApiResponse<MonthlySummaryDto>> {
    let params = new HttpParams().set('year', year).set('month', month);
    if (employeeId) params = params.set('employeeId', employeeId);
    return this.http.get<ApiResponse<MonthlySummaryDto>>(`${this.base}/reports/monthly-summary`, { params });
  }
  getExceptions(fromDate: string, toDate: string, type: string): Observable<ApiResponse<AttendanceExceptionsDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate).set('type', type);
    return this.http.get<ApiResponse<AttendanceExceptionsDto>>(`${this.base}/reports/exceptions`, { params });
  }

  // ── Office locations (admin) ──
  getOfficeLocations(): Observable<ApiResponse<OfficeLocationDto[]>> {
    return this.http.get<ApiResponse<OfficeLocationDto[]>>(this.locBase);
  }
  getOfficeLocationEmployees(id: number): Observable<ApiResponse<OfficeLocationEmployeeDto[]>> {
    return this.http.get<ApiResponse<OfficeLocationEmployeeDto[]>>(`${this.locBase}/${id}/employees`);
  }
  createOfficeLocation(data: UpsertOfficeLocation): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.locBase, data);
  }
  updateOfficeLocation(id: number, data: UpsertOfficeLocation): Observable<ApiResponse<number>> {
    return this.http.put<ApiResponse<number>>(`${this.locBase}/${id}`, data);
  }
  deleteOfficeLocation(id: number): Observable<ApiResponse<number>> {
    return this.http.delete<ApiResponse<number>>(`${this.locBase}/${id}`);
  }
  setOfficeLocationEmployees(id: number, employeeIds: number[]): Observable<ApiResponse<number>> {
    return this.http.put<ApiResponse<number>>(`${this.locBase}/${id}/employees`, { employeeIds });
  }
}
