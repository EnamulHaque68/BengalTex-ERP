import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PermissionGroupDto } from '../models/permission.models';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly base = `${environment.apiBaseUrl}/api/permissions`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<PermissionGroupDto[]>> {
    return this.http.get<ApiResponse<PermissionGroupDto[]>>(this.base);
  }
}
