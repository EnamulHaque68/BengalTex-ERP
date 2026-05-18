import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateReceiptRequest,
  ReceiptDto,
  ReceiptListItemDto
} from '../models/receipt.models';

@Injectable({ providedIn: 'root' })
export class ReceiptService {
  private readonly base = `${environment.apiBaseUrl}/api/receipts`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    customerInvoiceId?: number,
    customerId?: number,
    paymentMethod?: string
  ): Observable<ApiResponse<PagedResult<ReceiptListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (customerInvoiceId) params = params.set('customerInvoiceId', customerInvoiceId.toString());
    if (customerId) params = params.set('customerId', customerId.toString());
    if (paymentMethod) params = params.set('paymentMethod', paymentMethod);
    return this.http.get<ApiResponse<PagedResult<ReceiptListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<ReceiptDto>> {
    return this.http.get<ApiResponse<ReceiptDto>>(`${this.base}/${id}`);
  }

  create(data: CreateReceiptRequest): Observable<ApiResponse<ReceiptDto>> {
    return this.http.post<ApiResponse<ReceiptDto>>(this.base, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
