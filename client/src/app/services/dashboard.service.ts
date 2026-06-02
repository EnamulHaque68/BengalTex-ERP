import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { DashboardSnapshotDto } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly base = `${environment.apiBaseUrl}/api/dashboard`;
  constructor(private http: HttpClient) {}

  getSnapshot(): Observable<ApiResponse<DashboardSnapshotDto>> {
    return this.http.get<ApiResponse<DashboardSnapshotDto>>(`${this.base}/snapshot`);
  }
}
