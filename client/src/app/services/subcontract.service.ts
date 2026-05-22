import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateSubcontractOrderRequest, SubcontractOrderDto, SubcontractOrderListItemDto,
  SubcontractReceiveLineInput, UpdateSubcontractOrderRequest
} from '../models/subcontract.models';

@Injectable({ providedIn: 'root' })
export class SubcontractService {
  private readonly base = `${environment.apiBaseUrl}/api/subcontract-orders`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    subcontractorId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<SubcontractOrderListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (subcontractorId) params = params.set('subcontractorId', subcontractorId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<SubcontractOrderListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<SubcontractOrderDto>> {
    return this.http.get<ApiResponse<SubcontractOrderDto>>(`${this.base}/${id}`);
  }

  create(data: CreateSubcontractOrderRequest): Observable<ApiResponse<SubcontractOrderDto>> {
    return this.http.post<ApiResponse<SubcontractOrderDto>>(this.base, data);
  }

  update(id: number, data: UpdateSubcontractOrderRequest): Observable<ApiResponse<SubcontractOrderDto>> {
    return this.http.put<ApiResponse<SubcontractOrderDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  issue(id: number): Observable<ApiResponse<SubcontractOrderDto>> {
    return this.http.post<ApiResponse<SubcontractOrderDto>>(`${this.base}/${id}/issue`, {});
  }

  receive(id: number, lines: SubcontractReceiveLineInput[]): Observable<ApiResponse<SubcontractOrderDto>> {
    return this.http.post<ApiResponse<SubcontractOrderDto>>(`${this.base}/${id}/receive`, { lines });
  }

  cancel(id: number): Observable<ApiResponse<SubcontractOrderDto>> {
    return this.http.post<ApiResponse<SubcontractOrderDto>>(`${this.base}/${id}/cancel`, {});
  }
}
