import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { ApprovalRequestDto } from '../models/approval.models';

@Injectable({ providedIn: 'root' })
export class ApprovalService {
  private readonly base = `${environment.apiBaseUrl}/api/approvals`;

  constructor(private http: HttpClient) {}

  inbox(): Observable<ApiResponse<ApprovalRequestDto[]>> {
    return this.http.get<ApiResponse<ApprovalRequestDto[]>>(`${this.base}/inbox`);
  }

  getAll(status?: string): Observable<ApiResponse<ApprovalRequestDto[]>> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<ApprovalRequestDto[]>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<ApprovalRequestDto>> {
    return this.http.get<ApiResponse<ApprovalRequestDto>>(`${this.base}/${id}`);
  }

  approve(id: number, comment?: string | null): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.base}/${id}/approve`, { comment: comment ?? null });
  }

  reject(id: number, comment?: string | null): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.base}/${id}/reject`, { comment: comment ?? null });
  }
}
