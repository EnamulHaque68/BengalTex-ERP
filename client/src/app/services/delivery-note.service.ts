import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateDeliveryNoteRequest,
  DeliveryInvoiceLineInput,
  DeliveryNoteDto,
  DeliveryNoteListItemDto,
  UpdateDeliveryNoteRequest
} from '../models/delivery-note.models';

@Injectable({ providedIn: 'root' })
export class DeliveryNoteService {
  private readonly base = `${environment.apiBaseUrl}/api/delivery-notes`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    salesOrderId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<DeliveryNoteListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (salesOrderId) params = params.set('salesOrderId', salesOrderId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<DeliveryNoteListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<DeliveryNoteDto>> {
    return this.http.get<ApiResponse<DeliveryNoteDto>>(`${this.base}/${id}`);
  }

  create(data: CreateDeliveryNoteRequest): Observable<ApiResponse<DeliveryNoteDto>> {
    return this.http.post<ApiResponse<DeliveryNoteDto>>(this.base, data);
  }

  update(id: number, data: UpdateDeliveryNoteRequest): Observable<ApiResponse<DeliveryNoteDto>> {
    return this.http.put<ApiResponse<DeliveryNoteDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  post(id: number): Observable<ApiResponse<DeliveryNoteDto>> {
    return this.http.post<ApiResponse<DeliveryNoteDto>>(`${this.base}/${id}/post`, {});
  }

  /**
   * Generate a Draft customer invoice from this posted DN (returns { data: { id } }).
   * Pass `lines` to invoice only specific quantities (partial invoicing); omit to invoice
   * all remaining quantity.
   */
  createInvoice(
    id: number,
    lines?: DeliveryInvoiceLineInput[],
    vatRate = 0
  ): Observable<ApiResponse<{ id: number }>> {
    const url = `${environment.apiBaseUrl}/api/customer-invoices/from-delivery-note/${id}`;
    return this.http.post<ApiResponse<{ id: number }>>(url, { vatRate, lines: lines ?? null });
  }
}
