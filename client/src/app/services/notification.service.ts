import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import { NotificationDto, SendTestNotificationRequest } from '../models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly base = `${environment.apiBaseUrl}/api/notifications`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    channel?: string,
    status?: string
  ): Observable<ApiResponse<PagedResult<NotificationDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (channel) params = params.set('channel', channel);
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<NotificationDto>>>(this.base, { params });
  }

  sendTest(request: SendTestNotificationRequest): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.base}/test`, request);
  }
}
