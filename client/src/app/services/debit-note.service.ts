import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  DebitNoteDto, CreateDebitNoteRequest, UpdateDebitNoteRequest
} from '../models/credit-debit-note.models';

@Injectable({ providedIn: 'root' })
export class DebitNoteService {
  private readonly base = `${environment.apiBaseUrl}/api/debit-notes`;

  constructor(private http: HttpClient) {}

  getAll(p: PagedQueryParameters, status?: string, supplierId?: number, supplierInvoiceId?: number)
    : Observable<ApiResponse<PagedResult<DebitNoteDto>>> {
    let params = new HttpParams();
    if (p.page) params = params.set('Page', p.page.toString());
    if (p.pageSize) params = params.set('PageSize', p.pageSize.toString());
    if (p.search) params = params.set('Search', p.search);
    if (status) params = params.set('status', status);
    if (supplierId) params = params.set('supplierId', supplierId.toString());
    if (supplierInvoiceId) params = params.set('supplierInvoiceId', supplierInvoiceId.toString());
    return this.http.get<ApiResponse<PagedResult<DebitNoteDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<DebitNoteDto>> {
    return this.http.get<ApiResponse<DebitNoteDto>>(`${this.base}/${id}`);
  }

  create(data: CreateDebitNoteRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  update(id: number, data: UpdateDebitNoteRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  issue(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/issue`, {});
  }

  cancel(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/cancel`, {});
  }
}
