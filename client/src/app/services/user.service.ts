import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  CreateUserRequest,
  PagedQueryParameters,
  PagedResult,
  UpdateUserRequest,
  UserDto,
  UserListItemDto
} from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly base = `${environment.apiBaseUrl}/api/users`;

  constructor(private http: HttpClient) {}

  getAll(parameters: PagedQueryParameters): Observable<ApiResponse<PagedResult<UserListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    return this.http.get<ApiResponse<PagedResult<UserListItemDto>>>(this.base, { params });
  }

  getById(id: string): Observable<ApiResponse<UserDto>> {
    return this.http.get<ApiResponse<UserDto>>(`${this.base}/${id}`);
  }

  create(data: CreateUserRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(this.base, data);
  }

  update(id: string, data: UpdateUserRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/${id}`, data);
  }

  setActive(id: string, isActive: boolean): Observable<ApiResponse<null>> {
    return this.http.patch<ApiResponse<null>>(`${this.base}/${id}/active`, { isActive });
  }

  updateRoles(id: string, roles: string[]): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/${id}/roles`, { roles });
  }

  resetPassword(id: string, newPassword: string, confirmPassword: string): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/reset-password`, {
      newPassword,
      confirmPassword
    });
  }
}
