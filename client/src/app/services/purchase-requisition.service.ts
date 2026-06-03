import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  PurchaseRequisitionDto, CreatePurchaseRequisitionRequest,
  UpdatePurchaseRequisitionRequest, ConvertPrRequest
} from '../models/purchase-requisition.models';

@Injectable({ providedIn: 'root' })
export class PurchaseRequisitionService {
  private readonly base = `${environment.apiBaseUrl}/api/purchase-requisitions`;

  constructor(private http: HttpClient) {}

  getAll(p: PagedQueryParameters, status?: string, departmentId?: number)
    : Observable<ApiResponse<PagedResult<PurchaseRequisitionDto>>> {
    let params = new HttpParams();
    if (p.page) params = params.set('Page', p.page.toString());
    if (p.pageSize) params = params.set('PageSize', p.pageSize.toString());
    if (p.search) params = params.set('Search', p.search);
    if (status) params = params.set('status', status);
    if (departmentId) params = params.set('departmentId', departmentId.toString());
    return this.http.get<ApiResponse<PagedResult<PurchaseRequisitionDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<PurchaseRequisitionDto>> {
    return this.http.get<ApiResponse<PurchaseRequisitionDto>>(`${this.base}/${id}`);
  }

  create(data: CreatePurchaseRequisitionRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  update(id: number, data: UpdatePurchaseRequisitionRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  submit(id: number): Observable<ApiResponse<null>> { return this.http.post<ApiResponse<null>>(`${this.base}/${id}/submit`, {}); }
  approve(id: number, notes: string | null): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/approve`, { notes });
  }
  reject(id: number, notes: string | null): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/reject`, { notes });
  }
  cancel(id: number): Observable<ApiResponse<null>> { return this.http.post<ApiResponse<null>>(`${this.base}/${id}/cancel`, {}); }

  convert(id: number, data: ConvertPrRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/${id}/convert`, data);
  }
}
