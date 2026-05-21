import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreatePurchaseOrderRequest,
  PurchaseOrderDto,
  PurchaseOrderListItemDto,
  UpdatePurchaseOrderRequest
} from '../models/purchase-order.models';

@Injectable({ providedIn: 'root' })
export class PurchaseOrderService {
  private readonly base = `${environment.apiBaseUrl}/api/purchase-orders`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    supplierId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<PurchaseOrderListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (supplierId) params = params.set('supplierId', supplierId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<PurchaseOrderListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<PurchaseOrderDto>> {
    return this.http.get<ApiResponse<PurchaseOrderDto>>(`${this.base}/${id}`);
  }

  create(data: CreatePurchaseOrderRequest): Observable<ApiResponse<PurchaseOrderDto>> {
    return this.http.post<ApiResponse<PurchaseOrderDto>>(this.base, data);
  }

  update(id: number, data: UpdatePurchaseOrderRequest): Observable<ApiResponse<PurchaseOrderDto>> {
    return this.http.put<ApiResponse<PurchaseOrderDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  submitForApproval(id: number): Observable<ApiResponse<PurchaseOrderDto>> {
    return this.http.post<ApiResponse<PurchaseOrderDto>>(`${this.base}/${id}/submit-for-approval`, {});
  }

  send(id: number): Observable<ApiResponse<PurchaseOrderDto>> {
    return this.http.post<ApiResponse<PurchaseOrderDto>>(`${this.base}/${id}/send`, {});
  }

  cancel(id: number): Observable<ApiResponse<PurchaseOrderDto>> {
    return this.http.post<ApiResponse<PurchaseOrderDto>>(`${this.base}/${id}/cancel`, {});
  }

  close(id: number): Observable<ApiResponse<PurchaseOrderDto>> {
    return this.http.post<ApiResponse<PurchaseOrderDto>>(`${this.base}/${id}/close`, {});
  }
}
