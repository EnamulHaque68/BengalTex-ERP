import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  ExportIncentiveListDto, CreateExportIncentiveRequest, MarkIncentiveReceivedRequest
} from '../models/export-incentive.models';

/** Phase A6b — government export cash-incentive claims. */
@Injectable({ providedIn: 'root' })
export class ExportIncentiveService {
  private readonly base = `${environment.apiBaseUrl}/api/export-incentives`;

  constructor(private http: HttpClient) {}

  getAll(status?: string): Observable<ApiResponse<ExportIncentiveListDto>> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<ExportIncentiveListDto>>(this.base, { params });
  }

  create(body: CreateExportIncentiveRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, body);
  }

  markReceived(id: number, body: MarkIncentiveReceivedRequest): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/received`, body);
  }

  cancel(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/cancel`, {});
  }
}
