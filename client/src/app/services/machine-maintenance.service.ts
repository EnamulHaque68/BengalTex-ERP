import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  MachineMaintenanceDto, ScheduleMaintenanceRequest,
  UpdateMaintenanceRequest, CompleteMaintenanceRequest
} from '../models/machine-maintenance.models';

@Injectable({ providedIn: 'root' })
export class MachineMaintenanceService {
  private readonly base = `${environment.apiBaseUrl}/api/machine-maintenance`;

  constructor(private http: HttpClient) {}

  getAll(p: PagedQueryParameters, status?: string, type?: string, machineId?: number, fromDate?: string, toDate?: string)
    : Observable<ApiResponse<PagedResult<MachineMaintenanceDto>>> {
    let params = new HttpParams();
    if (p.page) params = params.set('Page', p.page.toString());
    if (p.pageSize) params = params.set('PageSize', p.pageSize.toString());
    if (p.search) params = params.set('Search', p.search);
    if (status) params = params.set('status', status);
    if (type) params = params.set('type', type);
    if (machineId) params = params.set('machineId', machineId.toString());
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<PagedResult<MachineMaintenanceDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<MachineMaintenanceDto>> {
    return this.http.get<ApiResponse<MachineMaintenanceDto>>(`${this.base}/${id}`);
  }

  schedule(data: ScheduleMaintenanceRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  update(id: number, data: UpdateMaintenanceRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  start(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/start`, {});
  }

  complete(id: number, data: CompleteMaintenanceRequest): Observable<ApiResponse<number | null>> {
    return this.http.post<ApiResponse<number | null>>(`${this.base}/${id}/complete`, data);
  }

  cancel(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/cancel`, {});
  }
}
