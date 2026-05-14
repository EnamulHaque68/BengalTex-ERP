import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateSupplierRequest,
  SupplierDto,
  SupplierListItemDto,
  UpdateSupplierRequest
} from '../models/supplier.models';

@Injectable({ providedIn: 'root' })
export class SupplierService {
  private readonly base = `${environment.apiBaseUrl}/api/suppliers`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    includeInactive = false
  ): Observable<ApiResponse<PagedResult<SupplierListItemDto>>> {
    let params = new HttpParams().set('includeInactive', includeInactive.toString());
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    return this.http.get<ApiResponse<PagedResult<SupplierListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<SupplierDto>> {
    return this.http.get<ApiResponse<SupplierDto>>(`${this.base}/${id}`);
  }

  create(data: CreateSupplierRequest): Observable<ApiResponse<SupplierDto>> {
    return this.http.post<ApiResponse<SupplierDto>>(this.base, data);
  }

  update(id: number, data: UpdateSupplierRequest): Observable<ApiResponse<SupplierDto>> {
    return this.http.put<ApiResponse<SupplierDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
