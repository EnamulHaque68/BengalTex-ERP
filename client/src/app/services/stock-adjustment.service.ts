import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateStockAdjustmentRequest,
  StockAdjustmentDto,
  StockAdjustmentListItemDto,
  UpdateStockAdjustmentRequest
} from '../models/inventory.models';

@Injectable({ providedIn: 'root' })
export class StockAdjustmentService {
  private readonly base = `${environment.apiBaseUrl}/api/stock-adjustments`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    warehouseId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<StockAdjustmentListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (warehouseId) params = params.set('warehouseId', warehouseId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<StockAdjustmentListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<StockAdjustmentDto>> {
    return this.http.get<ApiResponse<StockAdjustmentDto>>(`${this.base}/${id}`);
  }

  create(data: CreateStockAdjustmentRequest): Observable<ApiResponse<StockAdjustmentDto>> {
    return this.http.post<ApiResponse<StockAdjustmentDto>>(this.base, data);
  }

  update(id: number, data: UpdateStockAdjustmentRequest): Observable<ApiResponse<StockAdjustmentDto>> {
    return this.http.put<ApiResponse<StockAdjustmentDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  post(id: number): Observable<ApiResponse<StockAdjustmentDto>> {
    return this.http.post<ApiResponse<StockAdjustmentDto>>(`${this.base}/${id}/post`, {});
  }
}
