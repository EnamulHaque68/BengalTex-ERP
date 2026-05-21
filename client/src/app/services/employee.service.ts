import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateEmployeeRequest,
  EmployeeDto,
  EmployeeListItemDto,
  UpdateEmployeeRequest
} from '../models/employee.models';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly base = `${environment.apiBaseUrl}/api/employees`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    includeInactive = false,
    department?: string,
    status?: string
  ): Observable<ApiResponse<PagedResult<EmployeeListItemDto>>> {
    let params = new HttpParams().set('includeInactive', includeInactive.toString());
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (department) params = params.set('department', department);
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<EmployeeListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<EmployeeDto>> {
    return this.http.get<ApiResponse<EmployeeDto>>(`${this.base}/${id}`);
  }

  create(data: CreateEmployeeRequest): Observable<ApiResponse<EmployeeDto>> {
    return this.http.post<ApiResponse<EmployeeDto>>(this.base, data);
  }

  update(id: number, data: UpdateEmployeeRequest): Observable<ApiResponse<EmployeeDto>> {
    return this.http.put<ApiResponse<EmployeeDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
