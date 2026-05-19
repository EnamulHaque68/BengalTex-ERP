import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import { VatChallanDto, VatChallanListItemDto } from '../models/vat-challan.models';

@Injectable({ providedIn: 'root' })
export class VatChallanService {
  private readonly base = `${environment.apiBaseUrl}/api/vat-challans`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    customerId?: number,
    fromDate?: string,
    toDate?: string
  ): Observable<ApiResponse<PagedResult<VatChallanListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (customerId) params = params.set('customerId', customerId.toString());
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<PagedResult<VatChallanListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<VatChallanDto>> {
    return this.http.get<ApiResponse<VatChallanDto>>(`${this.base}/${id}`);
  }
}
