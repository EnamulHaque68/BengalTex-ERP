import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  OpeningStockEntryDto,
  OpeningStockListItemDto,
  SaveOpeningStockRequest
} from '../models/opening-stock.models';

@Injectable({ providedIn: 'root' })
export class OpeningStockService {
  private readonly base = `${environment.apiBaseUrl}/api/opening-stock`;

  constructor(private http: HttpClient) {}

  getAll(parameters: PagedQueryParameters, status?: string): Observable<ApiResponse<PagedResult<OpeningStockListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<OpeningStockListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<OpeningStockEntryDto>> {
    return this.http.get<ApiResponse<OpeningStockEntryDto>>(`${this.base}/${id}`);
  }

  create(data: SaveOpeningStockRequest): Observable<ApiResponse<OpeningStockEntryDto>> {
    return this.http.post<ApiResponse<OpeningStockEntryDto>>(this.base, data);
  }

  update(id: number, data: SaveOpeningStockRequest): Observable<ApiResponse<OpeningStockEntryDto>> {
    return this.http.put<ApiResponse<OpeningStockEntryDto>>(`${this.base}/${id}`, { id, ...data });
  }

  delete(id: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/${id}`);
  }

  post(id: number): Observable<ApiResponse<OpeningStockEntryDto>> {
    return this.http.post<ApiResponse<OpeningStockEntryDto>>(`${this.base}/${id}/post`, {});
  }
}
