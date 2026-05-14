import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateRawMaterialRequest,
  RawMaterialDto,
  RawMaterialListItemDto,
  UpdateRawMaterialRequest
} from '../models/raw-material.models';

@Injectable({ providedIn: 'root' })
export class RawMaterialService {
  private readonly base = `${environment.apiBaseUrl}/api/raw-materials`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    category?: string,
    includeInactive = false
  ): Observable<ApiResponse<PagedResult<RawMaterialListItemDto>>> {
    let params = new HttpParams().set('includeInactive', includeInactive.toString());
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (category) params = params.set('category', category);
    return this.http.get<ApiResponse<PagedResult<RawMaterialListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<RawMaterialDto>> {
    return this.http.get<ApiResponse<RawMaterialDto>>(`${this.base}/${id}`);
  }

  create(data: CreateRawMaterialRequest): Observable<ApiResponse<RawMaterialDto>> {
    return this.http.post<ApiResponse<RawMaterialDto>>(this.base, data);
  }

  update(id: number, data: UpdateRawMaterialRequest): Observable<ApiResponse<RawMaterialDto>> {
    return this.http.put<ApiResponse<RawMaterialDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
