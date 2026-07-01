import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateLetterOfCreditRequest, LetterOfCreditDto, LetterOfCreditListItemDto, UpdateLetterOfCreditRequest
} from '../models/letter-of-credit.models';

@Injectable({ providedIn: 'root' })
export class LetterOfCreditService {
  private readonly base = `${environment.apiBaseUrl}/api/letters-of-credit`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    supplierId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<LetterOfCreditListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (supplierId) params = params.set('supplierId', supplierId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<LetterOfCreditListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<LetterOfCreditDto>> {
    return this.http.get<ApiResponse<LetterOfCreditDto>>(`${this.base}/${id}`);
  }

  /** The LC linked to a PO (for goods-receipt auto-suggest); data is null if the PO has none. */
  getForPurchaseOrder(purchaseOrderId: number): Observable<ApiResponse<LetterOfCreditListItemDto | null>> {
    return this.http.get<ApiResponse<LetterOfCreditListItemDto | null>>(`${this.base}/for-po/${purchaseOrderId}`);
  }

  create(data: CreateLetterOfCreditRequest): Observable<ApiResponse<LetterOfCreditDto>> {
    return this.http.post<ApiResponse<LetterOfCreditDto>>(this.base, data);
  }

  update(id: number, data: UpdateLetterOfCreditRequest): Observable<ApiResponse<LetterOfCreditDto>> {
    return this.http.put<ApiResponse<LetterOfCreditDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  open(id: number): Observable<ApiResponse<LetterOfCreditDto>> {
    return this.http.post<ApiResponse<LetterOfCreditDto>>(`${this.base}/${id}/open`, {});
  }
  ship(id: number): Observable<ApiResponse<LetterOfCreditDto>> {
    return this.http.post<ApiResponse<LetterOfCreditDto>>(`${this.base}/${id}/ship`, {});
  }
  settle(id: number): Observable<ApiResponse<LetterOfCreditDto>> {
    return this.http.post<ApiResponse<LetterOfCreditDto>>(`${this.base}/${id}/settle`, {});
  }
  cancel(id: number): Observable<ApiResponse<LetterOfCreditDto>> {
    return this.http.post<ApiResponse<LetterOfCreditDto>>(`${this.base}/${id}/cancel`, {});
  }
}
