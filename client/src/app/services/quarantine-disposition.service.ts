import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateQuarantineDispositionRequest,
  QuarantineDispositionDto,
  QuarantineDispositionListItemDto,
  UpdateQuarantineDispositionRequest
} from '../models/quarantine-disposition.models';

@Injectable({ providedIn: 'root' })
export class QuarantineDispositionService {
  private readonly base = `${environment.apiBaseUrl}/api/quarantine-dispositions`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    dispositionType?: string,
    quarantineWarehouseId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<QuarantineDispositionListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (dispositionType) params = params.set('dispositionType', dispositionType);
    if (quarantineWarehouseId) params = params.set('quarantineWarehouseId', quarantineWarehouseId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<QuarantineDispositionListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<QuarantineDispositionDto>> {
    return this.http.get<ApiResponse<QuarantineDispositionDto>>(`${this.base}/${id}`);
  }

  create(data: CreateQuarantineDispositionRequest): Observable<ApiResponse<QuarantineDispositionDto>> {
    return this.http.post<ApiResponse<QuarantineDispositionDto>>(this.base, data);
  }

  update(id: number, data: UpdateQuarantineDispositionRequest): Observable<ApiResponse<QuarantineDispositionDto>> {
    return this.http.put<ApiResponse<QuarantineDispositionDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  post(id: number): Observable<ApiResponse<QuarantineDispositionDto>> {
    return this.http.post<ApiResponse<QuarantineDispositionDto>>(`${this.base}/${id}/post`, {});
  }
}
