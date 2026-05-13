import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  CreateWarehouseRequest,
  UpdateWarehouseRequest,
  WarehouseDto
} from '../models/master-data.models';

@Injectable({ providedIn: 'root' })
export class WarehouseService {
  private readonly base = `${environment.apiBaseUrl}/api/warehouses`;

  constructor(private http: HttpClient) {}

  getAll(factoryId?: number, includeInactive = false): Observable<ApiResponse<WarehouseDto[]>> {
    let params = new HttpParams().set('includeInactive', includeInactive.toString());
    if (factoryId) params = params.set('factoryId', factoryId.toString());
    return this.http.get<ApiResponse<WarehouseDto[]>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<WarehouseDto>> {
    return this.http.get<ApiResponse<WarehouseDto>>(`${this.base}/${id}`);
  }

  create(data: CreateWarehouseRequest): Observable<ApiResponse<WarehouseDto>> {
    return this.http.post<ApiResponse<WarehouseDto>>(this.base, data);
  }

  update(id: number, data: UpdateWarehouseRequest): Observable<ApiResponse<WarehouseDto>> {
    return this.http.put<ApiResponse<WarehouseDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
