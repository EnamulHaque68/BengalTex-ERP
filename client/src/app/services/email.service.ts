import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  SentEmailDto, EmailPreviewDto, SendDocumentEmailRequest
} from '../models/email.models';

@Injectable({ providedIn: 'root' })
export class EmailService {
  private readonly base = `${environment.apiBaseUrl}/api/emails`;

  constructor(private http: HttpClient) {}

  getAll(p: PagedQueryParameters, status?: string, sourceType?: string, sourceId?: number)
    : Observable<ApiResponse<PagedResult<SentEmailDto>>> {
    let params = new HttpParams();
    if (p.page) params = params.set('Page', p.page.toString());
    if (p.pageSize) params = params.set('PageSize', p.pageSize.toString());
    if (p.search) params = params.set('Search', p.search);
    if (status) params = params.set('status', status);
    if (sourceType) params = params.set('sourceType', sourceType);
    if (sourceId) params = params.set('sourceId', sourceId.toString());
    return this.http.get<ApiResponse<PagedResult<SentEmailDto>>>(this.base, { params });
  }

  preview(sourceType: string, sourceId: number): Observable<ApiResponse<EmailPreviewDto>> {
    const params = new HttpParams().set('sourceType', sourceType).set('sourceId', sourceId.toString());
    return this.http.get<ApiResponse<EmailPreviewDto>>(`${this.base}/preview`, { params });
  }

  send(data: SendDocumentEmailRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/send-document`, data);
  }
}
