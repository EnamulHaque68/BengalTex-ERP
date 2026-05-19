import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateSupplierInvoiceRequest,
  SupplierInvoiceDto,
  SupplierInvoiceListItemDto,
  UpdateSupplierInvoiceRequest
} from '../models/supplier-invoice.models';

@Injectable({ providedIn: 'root' })
export class SupplierInvoiceService {
  private readonly base = `${environment.apiBaseUrl}/api/supplier-invoices`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    supplierId?: number,
    purchaseOrderId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<SupplierInvoiceListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (supplierId) params = params.set('supplierId', supplierId.toString());
    if (purchaseOrderId) params = params.set('purchaseOrderId', purchaseOrderId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<SupplierInvoiceListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<SupplierInvoiceDto>> {
    return this.http.get<ApiResponse<SupplierInvoiceDto>>(`${this.base}/${id}`);
  }

  create(data: CreateSupplierInvoiceRequest): Observable<ApiResponse<SupplierInvoiceDto>> {
    return this.http.post<ApiResponse<SupplierInvoiceDto>>(this.base, data);
  }

  update(id: number, data: UpdateSupplierInvoiceRequest): Observable<ApiResponse<SupplierInvoiceDto>> {
    return this.http.put<ApiResponse<SupplierInvoiceDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  approve(id: number): Observable<ApiResponse<SupplierInvoiceDto>> {
    return this.http.post<ApiResponse<SupplierInvoiceDto>>(`${this.base}/${id}/approve`, {});
  }

  cancel(id: number): Observable<ApiResponse<SupplierInvoiceDto>> {
    return this.http.post<ApiResponse<SupplierInvoiceDto>>(`${this.base}/${id}/cancel`, {});
  }
}
