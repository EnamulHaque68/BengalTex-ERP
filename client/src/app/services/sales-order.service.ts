import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateSalesOrderRequest,
  SalesOrderDto,
  SalesOrderListItemDto,
  UpdateSalesOrderRequest
} from '../models/sales-order.models';

@Injectable({ providedIn: 'root' })
export class SalesOrderService {
  private readonly base = `${environment.apiBaseUrl}/api/sales-orders`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    customerId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<SalesOrderListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (customerId) params = params.set('customerId', customerId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<SalesOrderListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<SalesOrderDto>> {
    return this.http.get<ApiResponse<SalesOrderDto>>(`${this.base}/${id}`);
  }

  create(data: CreateSalesOrderRequest): Observable<ApiResponse<SalesOrderDto>> {
    return this.http.post<ApiResponse<SalesOrderDto>>(this.base, data);
  }

  update(id: number, data: UpdateSalesOrderRequest): Observable<ApiResponse<SalesOrderDto>> {
    return this.http.put<ApiResponse<SalesOrderDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  confirm(id: number): Observable<ApiResponse<SalesOrderDto>> {
    return this.http.post<ApiResponse<SalesOrderDto>>(`${this.base}/${id}/confirm`, {});
  }

  cancel(id: number): Observable<ApiResponse<SalesOrderDto>> {
    return this.http.post<ApiResponse<SalesOrderDto>>(`${this.base}/${id}/cancel`, {});
  }
}
