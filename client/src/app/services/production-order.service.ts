import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateProductionOrderRequest,
  ProductionOrderDto,
  ProductionOrderListItemDto,
  UpdateProductionOrderRequest
} from '../models/production-order.models';

@Injectable({ providedIn: 'root' })
export class ProductionOrderService {
  private readonly base = `${environment.apiBaseUrl}/api/production-orders`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    productId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<ProductionOrderListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (productId) params = params.set('productId', productId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<ProductionOrderListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.get<ApiResponse<ProductionOrderDto>>(`${this.base}/${id}`);
  }

  create(data: CreateProductionOrderRequest): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(this.base, data);
  }

  update(id: number, data: UpdateProductionOrderRequest): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.put<ApiResponse<ProductionOrderDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  start(id: number): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(`${this.base}/${id}/start`, {});
  }

  complete(id: number): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(`${this.base}/${id}/complete`, {});
  }

  cancel(id: number): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(`${this.base}/${id}/cancel`, {});
  }
}
