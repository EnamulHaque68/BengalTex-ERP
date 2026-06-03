import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import { GatePassDto, SaveGatePassRequest } from '../models/gate-pass.models';

@Injectable({ providedIn: 'root' })
export class GatePassService {
  private readonly base = `${environment.apiBaseUrl}/api/gate-passes`;

  constructor(private http: HttpClient) {}

  getAll(p: PagedQueryParameters, status?: string, type?: string, fromDate?: string, toDate?: string)
    : Observable<ApiResponse<PagedResult<GatePassDto>>> {
    let params = new HttpParams();
    if (p.page) params = params.set('Page', p.page.toString());
    if (p.pageSize) params = params.set('PageSize', p.pageSize.toString());
    if (p.search) params = params.set('Search', p.search);
    if (status) params = params.set('status', status);
    if (type) params = params.set('type', type);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<PagedResult<GatePassDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<GatePassDto>> {
    return this.http.get<ApiResponse<GatePassDto>>(`${this.base}/${id}`);
  }

  create(data: SaveGatePassRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  update(id: number, data: SaveGatePassRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  close(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/close`, {});
  }

  markReturned(id: number, returnNotes: string | null): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/mark-returned`, { returnNotes });
  }

  cancel(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/cancel`, {});
  }
}
