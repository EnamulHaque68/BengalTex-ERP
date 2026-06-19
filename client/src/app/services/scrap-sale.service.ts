import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  ScrapSaleDto,
  ScrapSaleListItemDto,
  SaveScrapSaleRequest
} from '../models/scrap-sale.models';

@Injectable({ providedIn: 'root' })
export class ScrapSaleService {
  private readonly base = `${environment.apiBaseUrl}/api/scrap-sales`;

  constructor(private http: HttpClient) {}

  getAll(parameters: PagedQueryParameters, status?: string): Observable<ApiResponse<PagedResult<ScrapSaleListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<ScrapSaleListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<ScrapSaleDto>> {
    return this.http.get<ApiResponse<ScrapSaleDto>>(`${this.base}/${id}`);
  }

  create(data: SaveScrapSaleRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  update(id: number, data: SaveScrapSaleRequest): Observable<ApiResponse<number>> {
    return this.http.put<ApiResponse<number>>(`${this.base}/${id}`, { id, ...data });
  }

  delete(id: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/${id}`);
  }

  post(id: number): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/${id}/post`, {});
  }
}
