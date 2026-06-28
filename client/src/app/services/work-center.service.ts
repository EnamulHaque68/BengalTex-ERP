import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { WorkCenterDto, CreateWorkCenterRequest, UpdateWorkCenterRequest } from '../models/work-center.models';

@Injectable({ providedIn: 'root' })
export class WorkCenterService {
  private readonly base = `${environment.apiBaseUrl}/api/work-centers`;

  constructor(private http: HttpClient) {}

  getAll(includeInactive = false): Observable<ApiResponse<WorkCenterDto[]>> {
    let params = new HttpParams();
    if (includeInactive) params = params.set('includeInactive', 'true');
    return this.http.get<ApiResponse<WorkCenterDto[]>>(this.base, { params });
  }

  create(data: CreateWorkCenterRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  update(id: number, data: UpdateWorkCenterRequest): Observable<ApiResponse<number>> {
    return this.http.put<ApiResponse<number>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
