import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import { AuditLogEntryDto } from '../models/audit-log.models';

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly base = `${environment.apiBaseUrl}/api/audit-log`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    entityType?: string,
    action?: string,
    userName?: string,
    fromDate?: string,
    toDate?: string
  ): Observable<ApiResponse<PagedResult<AuditLogEntryDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (entityType) params = params.set('entityType', entityType);
    if (action) params = params.set('action', action);
    if (userName) params = params.set('userName', userName);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<PagedResult<AuditLogEntryDto>>>(this.base, { params });
  }

  getEntityTypes(): Observable<ApiResponse<string[]>> {
    return this.http.get<ApiResponse<string[]>>(`${this.base}/entity-types`);
  }
}
