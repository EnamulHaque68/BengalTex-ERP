import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateCustomerReturnNoteRequest,
  CustomerReturnNoteDto,
  CustomerReturnNoteListItemDto,
  UpdateCustomerReturnNoteRequest
} from '../models/customer-return-note.models';

@Injectable({ providedIn: 'root' })
export class CustomerReturnNoteService {
  private readonly base = `${environment.apiBaseUrl}/api/customer-return-notes`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    deliveryNoteId?: number,
    customerId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<CustomerReturnNoteListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (deliveryNoteId) params = params.set('deliveryNoteId', deliveryNoteId.toString());
    if (customerId) params = params.set('customerId', customerId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<CustomerReturnNoteListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<CustomerReturnNoteDto>> {
    return this.http.get<ApiResponse<CustomerReturnNoteDto>>(`${this.base}/${id}`);
  }

  create(data: CreateCustomerReturnNoteRequest): Observable<ApiResponse<CustomerReturnNoteDto>> {
    return this.http.post<ApiResponse<CustomerReturnNoteDto>>(this.base, data);
  }

  update(id: number, data: UpdateCustomerReturnNoteRequest): Observable<ApiResponse<CustomerReturnNoteDto>> {
    return this.http.put<ApiResponse<CustomerReturnNoteDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  post(id: number): Observable<ApiResponse<CustomerReturnNoteDto>> {
    return this.http.post<ApiResponse<CustomerReturnNoteDto>>(`${this.base}/${id}/post`, {});
  }
}
