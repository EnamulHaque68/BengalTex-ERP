import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateQcInspectionRequest,
  QcInspectionDto,
  QcInspectionListItemDto,
  UpdateQcInspectionRequest
} from '../models/qc-inspection.models';

@Injectable({ providedIn: 'root' })
export class QcInspectionService {
  private readonly base = `${environment.apiBaseUrl}/api/qc-inspections`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    sourceType?: string,
    status?: string,
    result?: string
  ): Observable<ApiResponse<PagedResult<QcInspectionListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (sourceType) params = params.set('sourceType', sourceType);
    if (status) params = params.set('status', status);
    if (result) params = params.set('result', result);
    return this.http.get<ApiResponse<PagedResult<QcInspectionListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<QcInspectionDto>> {
    return this.http.get<ApiResponse<QcInspectionDto>>(`${this.base}/${id}`);
  }

  create(data: CreateQcInspectionRequest): Observable<ApiResponse<QcInspectionDto>> {
    return this.http.post<ApiResponse<QcInspectionDto>>(this.base, data);
  }

  update(id: number, data: UpdateQcInspectionRequest): Observable<ApiResponse<QcInspectionDto>> {
    return this.http.put<ApiResponse<QcInspectionDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  post(id: number): Observable<ApiResponse<QcInspectionDto>> {
    return this.http.post<ApiResponse<QcInspectionDto>>(`${this.base}/${id}/post`, {});
  }
}
