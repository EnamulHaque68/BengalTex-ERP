import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import { QuotationDto, QuotationListItemDto, SaveQuotationRequest } from '../models/quotation.models';

@Injectable({ providedIn: 'root' })
export class QuotationService {
  private readonly base = `${environment.apiBaseUrl}/api/quotations`;

  constructor(private http: HttpClient) {}

  getAll(parameters: PagedQueryParameters, customerId?: number, status?: string)
    : Observable<ApiResponse<PagedResult<QuotationListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (customerId) params = params.set('customerId', customerId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<QuotationListItemDto>>>(this.base, { params });
  }
  getById(id: number): Observable<ApiResponse<QuotationDto>> {
    return this.http.get<ApiResponse<QuotationDto>>(`${this.base}/${id}`);
  }
  create(data: SaveQuotationRequest): Observable<ApiResponse<QuotationDto>> {
    return this.http.post<ApiResponse<QuotationDto>>(this.base, data);
  }
  update(id: number, data: SaveQuotationRequest): Observable<ApiResponse<QuotationDto>> {
    return this.http.put<ApiResponse<QuotationDto>>(`${this.base}/${id}`, data);
  }
  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
  send(id: number): Observable<ApiResponse<QuotationDto>> { return this.http.post<ApiResponse<QuotationDto>>(`${this.base}/${id}/send`, {}); }
  accept(id: number): Observable<ApiResponse<QuotationDto>> { return this.http.post<ApiResponse<QuotationDto>>(`${this.base}/${id}/accept`, {}); }
  reject(id: number): Observable<ApiResponse<QuotationDto>> { return this.http.post<ApiResponse<QuotationDto>>(`${this.base}/${id}/reject`, {}); }
  revise(id: number): Observable<ApiResponse<QuotationDto>> { return this.http.post<ApiResponse<QuotationDto>>(`${this.base}/${id}/revise`, {}); }
  convert(id: number): Observable<ApiResponse<QuotationDto>> { return this.http.post<ApiResponse<QuotationDto>>(`${this.base}/${id}/convert`, {}); }
}
