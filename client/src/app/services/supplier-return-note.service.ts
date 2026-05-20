import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateSupplierReturnNoteRequest,
  SupplierReturnNoteDto,
  SupplierReturnNoteListItemDto,
  UpdateSupplierReturnNoteRequest
} from '../models/supplier-return-note.models';

@Injectable({ providedIn: 'root' })
export class SupplierReturnNoteService {
  private readonly base = `${environment.apiBaseUrl}/api/supplier-return-notes`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    goodsReceiptNoteId?: number,
    supplierId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<SupplierReturnNoteListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (goodsReceiptNoteId) params = params.set('goodsReceiptNoteId', goodsReceiptNoteId.toString());
    if (supplierId) params = params.set('supplierId', supplierId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<SupplierReturnNoteListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<SupplierReturnNoteDto>> {
    return this.http.get<ApiResponse<SupplierReturnNoteDto>>(`${this.base}/${id}`);
  }

  create(data: CreateSupplierReturnNoteRequest): Observable<ApiResponse<SupplierReturnNoteDto>> {
    return this.http.post<ApiResponse<SupplierReturnNoteDto>>(this.base, data);
  }

  update(id: number, data: UpdateSupplierReturnNoteRequest): Observable<ApiResponse<SupplierReturnNoteDto>> {
    return this.http.put<ApiResponse<SupplierReturnNoteDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  post(id: number): Observable<ApiResponse<SupplierReturnNoteDto>> {
    return this.http.post<ApiResponse<SupplierReturnNoteDto>>(`${this.base}/${id}/post`, {});
  }
}
