import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  AttendanceRecordDto,
  CreateAttendanceRequest,
  UpdateAttendanceRequest
} from '../models/attendance.models';

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly base = `${environment.apiBaseUrl}/api/attendance`;

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
}
