import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateCustomerInvoiceRequest,
  CustomerInvoiceDto,
  CustomerInvoiceListItemDto,
  UpdateCustomerInvoiceRequest
} from '../models/customer-invoice.models';

@Injectable({ providedIn: 'root' })
export class CustomerInvoiceService {
  private readonly base = `${environment.apiBaseUrl}/api/customer-invoices`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    customerId?: number,
    salesOrderId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<CustomerInvoiceListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (customerId) params = params.set('customerId', customerId.toString());
    if (salesOrderId) params = params.set('salesOrderId', salesOrderId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<CustomerInvoiceListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<CustomerInvoiceDto>> {
    return this.http.get<ApiResponse<CustomerInvoiceDto>>(`${this.base}/${id}`);
  }

  create(data: CreateCustomerInvoiceRequest): Observable<ApiResponse<CustomerInvoiceDto>> {
    return this.http.post<ApiResponse<CustomerInvoiceDto>>(this.base, data);
  }

  update(id: number, data: UpdateCustomerInvoiceRequest): Observable<ApiResponse<CustomerInvoiceDto>> {
    return this.http.put<ApiResponse<CustomerInvoiceDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  issue(id: number): Observable<ApiResponse<CustomerInvoiceDto>> {
    return this.http.post<ApiResponse<CustomerInvoiceDto>>(`${this.base}/${id}/issue`, {});
  }

  cancel(id: number): Observable<ApiResponse<CustomerInvoiceDto>> {
    return this.http.post<ApiResponse<CustomerInvoiceDto>>(`${this.base}/${id}/cancel`, {});
  }
}
