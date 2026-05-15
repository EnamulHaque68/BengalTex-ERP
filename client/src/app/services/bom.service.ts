import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  BomDto,
  BomListItemDto,
  CreateBomRequest,
  UpdateBomRequest
} from '../models/bom.models';

@Injectable({ providedIn: 'root' })
export class BomService {
  private readonly base = `${environment.apiBaseUrl}/api/boms`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    productId?: number,
    status?: string,
    activeOnly = false
  ): Observable<ApiResponse<PagedResult<BomListItemDto>>> {
    let params = new HttpParams().set('activeOnly', activeOnly.toString());
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (productId) params = params.set('productId', productId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<BomListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<BomDto>> {
    return this.http.get<ApiResponse<BomDto>>(`${this.base}/${id}`);
  }

  create(data: CreateBomRequest): Observable<ApiResponse<BomDto>> {
    return this.http.post<ApiResponse<BomDto>>(this.base, data);
  }

  update(id: number, data: UpdateBomRequest): Observable<ApiResponse<BomDto>> {
    return this.http.put<ApiResponse<BomDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  approve(id: number): Observable<ApiResponse<BomDto>> {
    return this.http.post<ApiResponse<BomDto>>(`${this.base}/${id}/approve`, {});
  }

  activate(id: number): Observable<ApiResponse<BomDto>> {
    return this.http.post<ApiResponse<BomDto>>(`${this.base}/${id}/activate`, {});
  }
}
