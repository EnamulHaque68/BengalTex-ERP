import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { RoleDto, RoleListItemDto } from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly base = `${environment.apiBaseUrl}/api/roles`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<RoleListItemDto[]>> {
    return this.http.get<ApiResponse<RoleListItemDto[]>>(this.base);
  }

  getById(id: string): Observable<ApiResponse<RoleDto>> {
    return this.http.get<ApiResponse<RoleDto>>(`${this.base}/${id}`);
  }

  create(name: string, description: string | null): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(this.base, { name, description });
  }

  update(id: string, name: string, description: string | null): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/${id}`, { name, description });
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  updatePermissions(id: string, permissions: string[]): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/${id}/permissions`, { permissions });
  }
}
