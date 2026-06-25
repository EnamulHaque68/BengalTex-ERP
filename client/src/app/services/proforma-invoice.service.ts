import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  ProformaInvoiceDto, CreateProformaInvoiceRequest, UpdateProformaInvoiceRequest, ConvertProformaRequest,
  ConvertProformaToSoRequest
} from '../models/proforma-invoice.models';

@Injectable({ providedIn: 'root' })
export class ProformaInvoiceService {
  private readonly base = `${environment.apiBaseUrl}/api/proforma-invoices`;

  constructor(private http: HttpClient) {}

  getAll(p: PagedQueryParameters, status?: string, customerId?: number)
    : Observable<ApiResponse<PagedResult<ProformaInvoiceDto>>> {
    let params = new HttpParams();
    if (p.page) params = params.set('Page', p.page.toString());
    if (p.pageSize) params = params.set('PageSize', p.pageSize.toString());
    if (p.search) params = params.set('Search', p.search);
    if (status) params = params.set('status', status);
    if (customerId) params = params.set('customerId', customerId.toString());
    return this.http.get<ApiResponse<PagedResult<ProformaInvoiceDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<ProformaInvoiceDto>> {
    return this.http.get<ApiResponse<ProformaInvoiceDto>>(`${this.base}/${id}`);
  }

  create(data: CreateProformaInvoiceRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  update(id: number, data: UpdateProformaInvoiceRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  send(id: number): Observable<ApiResponse<null>>   { return this.http.post<ApiResponse<null>>(`${this.base}/${id}/send`, {}); }
  accept(id: number): Observable<ApiResponse<null>> { return this.http.post<ApiResponse<null>>(`${this.base}/${id}/accept`, {}); }
  expire(id: number): Observable<ApiResponse<null>> { return this.http.post<ApiResponse<null>>(`${this.base}/${id}/expire`, {}); }
  cancel(id: number): Observable<ApiResponse<null>> { return this.http.post<ApiResponse<null>>(`${this.base}/${id}/cancel`, {}); }

  convert(id: number, data: ConvertProformaRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/${id}/convert`, data);
  }

  convertToSalesOrder(id: number, data: ConvertProformaToSoRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/${id}/convert-to-sales-order`, data);
  }

  /** Uploads a confirmation document → returns the storage path to include in convertToSalesOrder. */
  uploadConfirmation(id: number, file: File): Observable<ApiResponse<{ storagePath: string }> | { storagePath: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ storagePath: string }>(`${this.base}/${id}/confirmation-attachment`, form);
  }

  confirmationUrl(id: number): string { return `${this.base}/${id}/confirmation-attachment`; }
}
