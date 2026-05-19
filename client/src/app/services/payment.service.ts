import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreatePaymentRequest,
  PaymentDto,
  PaymentListItemDto
} from '../models/payment.models';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly base = `${environment.apiBaseUrl}/api/payments`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    supplierInvoiceId?: number,
    supplierId?: number,
    paymentMethod?: string
  ): Observable<ApiResponse<PagedResult<PaymentListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (supplierInvoiceId) params = params.set('supplierInvoiceId', supplierInvoiceId.toString());
    if (supplierId) params = params.set('supplierId', supplierId.toString());
    if (paymentMethod) params = params.set('paymentMethod', paymentMethod);
    return this.http.get<ApiResponse<PagedResult<PaymentListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<PaymentDto>> {
    return this.http.get<ApiResponse<PaymentDto>>(`${this.base}/${id}`);
  }

  create(data: CreatePaymentRequest): Observable<ApiResponse<PaymentDto>> {
    return this.http.post<ApiResponse<PaymentDto>>(this.base, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
