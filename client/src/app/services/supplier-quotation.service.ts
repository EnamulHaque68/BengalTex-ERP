import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  SupplierQuotationDto,
  SupplierQuotationListItemDto,
  SaveSupplierQuotationRequest,
  QuotationComparisonDto
} from '../models/supplier-quotation.models';

@Injectable({ providedIn: 'root' })
export class SupplierQuotationService {
  private readonly base = `${environment.apiBaseUrl}/api/supplier-quotations`;

  constructor(private http: HttpClient) {}

  getAll(parameters: PagedQueryParameters, status?: string, supplierId?: number, purchaseRequisitionId?: number)
    : Observable<ApiResponse<PagedResult<SupplierQuotationListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (status) params = params.set('status', status);
    if (supplierId) params = params.set('supplierId', supplierId.toString());
    if (purchaseRequisitionId) params = params.set('purchaseRequisitionId', purchaseRequisitionId.toString());
    return this.http.get<ApiResponse<PagedResult<SupplierQuotationListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<SupplierQuotationDto>> {
    return this.http.get<ApiResponse<SupplierQuotationDto>>(`${this.base}/${id}`);
  }

  getComparison(purchaseRequisitionId: number): Observable<ApiResponse<QuotationComparisonDto>> {
    return this.http.get<ApiResponse<QuotationComparisonDto>>(`${this.base}/comparison/${purchaseRequisitionId}`);
  }

  create(data: SaveSupplierQuotationRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  update(id: number, data: SaveSupplierQuotationRequest): Observable<ApiResponse<number>> {
    return this.http.put<ApiResponse<number>>(`${this.base}/${id}`, { id, ...data });
  }

  delete(id: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/${id}`);
  }

  submit(id: number): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(`${this.base}/${id}/submit`, {}); }
  reject(id: number): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(`${this.base}/${id}/reject`, {}); }
  select(id: number): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(`${this.base}/${id}/select`, {}); }
}
