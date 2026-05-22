import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateStyleRequest, StyleDto, StyleListItemDto, UpdateStyleRequest
} from '../models/style.models';

@Injectable({ providedIn: 'root' })
export class StyleService {
  private readonly base = `${environment.apiBaseUrl}/api/styles`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    includeInactive = false,
    buyerId?: number,
    status?: string
  ): Observable<ApiResponse<PagedResult<StyleListItemDto>>> {
    let params = new HttpParams().set('includeInactive', includeInactive.toString());
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (buyerId) params = params.set('buyerId', buyerId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<StyleListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<StyleDto>> {
    return this.http.get<ApiResponse<StyleDto>>(`${this.base}/${id}`);
  }

  create(data: CreateStyleRequest): Observable<ApiResponse<StyleDto>> {
    return this.http.post<ApiResponse<StyleDto>>(this.base, data);
  }

  update(id: number, data: UpdateStyleRequest): Observable<ApiResponse<StyleDto>> {
    return this.http.put<ApiResponse<StyleDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
