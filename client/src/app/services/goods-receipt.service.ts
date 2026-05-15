import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateGoodsReceiptRequest,
  GoodsReceiptDto,
  GoodsReceiptListItemDto,
  UpdateGoodsReceiptRequest
} from '../models/goods-receipt.models';

@Injectable({ providedIn: 'root' })
export class GoodsReceiptService {
  private readonly base = `${environment.apiBaseUrl}/api/goods-receipts`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    purchaseOrderId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<GoodsReceiptListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (purchaseOrderId) params = params.set('purchaseOrderId', purchaseOrderId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<GoodsReceiptListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<GoodsReceiptDto>> {
    return this.http.get<ApiResponse<GoodsReceiptDto>>(`${this.base}/${id}`);
  }

  create(data: CreateGoodsReceiptRequest): Observable<ApiResponse<GoodsReceiptDto>> {
    return this.http.post<ApiResponse<GoodsReceiptDto>>(this.base, data);
  }

  update(id: number, data: UpdateGoodsReceiptRequest): Observable<ApiResponse<GoodsReceiptDto>> {
    return this.http.put<ApiResponse<GoodsReceiptDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  post(id: number): Observable<ApiResponse<GoodsReceiptDto>> {
    return this.http.post<ApiResponse<GoodsReceiptDto>>(`${this.base}/${id}/post`, {});
  }
}
