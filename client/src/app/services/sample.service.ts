import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import { SampleDto, SampleListItemDto, SaveSampleRequest } from '../models/sample.models';

@Injectable({ providedIn: 'root' })
export class SampleService {
  private readonly base = `${environment.apiBaseUrl}/api/samples`;

  constructor(private http: HttpClient) {}

  getAll(parameters: PagedQueryParameters, customerId?: number, status?: string)
    : Observable<ApiResponse<PagedResult<SampleListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (customerId) params = params.set('customerId', customerId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<SampleListItemDto>>>(this.base, { params });
  }
  getById(id: number): Observable<ApiResponse<SampleDto>> {
    return this.http.get<ApiResponse<SampleDto>>(`${this.base}/${id}`);
  }
  create(data: SaveSampleRequest): Observable<ApiResponse<SampleDto>> {
    return this.http.post<ApiResponse<SampleDto>>(this.base, data);
  }
  update(id: number, data: SaveSampleRequest): Observable<ApiResponse<SampleDto>> {
    return this.http.put<ApiResponse<SampleDto>>(`${this.base}/${id}`, data);
  }
  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
  startDevelopment(id: number): Observable<ApiResponse<SampleDto>> { return this.http.post<ApiResponse<SampleDto>>(`${this.base}/${id}/start-development`, {}); }
  submit(id: number): Observable<ApiResponse<SampleDto>> { return this.http.post<ApiResponse<SampleDto>>(`${this.base}/${id}/submit`, {}); }
  decide(id: number, approve: boolean, feedback: string | null): Observable<ApiResponse<SampleDto>> {
    return this.http.post<ApiResponse<SampleDto>>(`${this.base}/${id}/decide`, { approve, feedback });
  }
}
