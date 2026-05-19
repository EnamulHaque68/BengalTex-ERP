import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateStockTransferRequest,
  StockTransferDto,
  StockTransferListItemDto,
  UpdateStockTransferRequest
} from '../models/stock-transfer.models';

@Injectable({ providedIn: 'root' })
export class StockTransferService {
  private readonly base = `${environment.apiBaseUrl}/api/stock-transfers`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    sourceWarehouseId?: number,
    destinationWarehouseId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<StockTransferListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (sourceWarehouseId) params = params.set('sourceWarehouseId', sourceWarehouseId.toString());
    if (destinationWarehouseId) params = params.set('destinationWarehouseId', destinationWarehouseId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<StockTransferListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<StockTransferDto>> {
    return this.http.get<ApiResponse<StockTransferDto>>(`${this.base}/${id}`);
  }

  create(data: CreateStockTransferRequest): Observable<ApiResponse<StockTransferDto>> {
    return this.http.post<ApiResponse<StockTransferDto>>(this.base, data);
  }

  update(id: number, data: UpdateStockTransferRequest): Observable<ApiResponse<StockTransferDto>> {
    return this.http.put<ApiResponse<StockTransferDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  post(id: number): Observable<ApiResponse<StockTransferDto>> {
    return this.http.post<ApiResponse<StockTransferDto>>(`${this.base}/${id}/post`, {});
  }
}
