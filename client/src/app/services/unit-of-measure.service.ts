import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  CreateUnitOfMeasureRequest,
  UnitOfMeasureDto,
  UpdateUnitOfMeasureRequest
} from '../models/master-data.models';

@Injectable({ providedIn: 'root' })
export class UnitOfMeasureService {
  private readonly base = `${environment.apiBaseUrl}/api/units-of-measure`;

  constructor(private http: HttpClient) {}

  getAll(includeInactive = false): Observable<ApiResponse<UnitOfMeasureDto[]>> {
    return this.http.get<ApiResponse<UnitOfMeasureDto[]>>(this.base, {
      params: new HttpParams().set('includeInactive', includeInactive.toString())
    });
  }

  getById(id: number): Observable<ApiResponse<UnitOfMeasureDto>> {
    return this.http.get<ApiResponse<UnitOfMeasureDto>>(`${this.base}/${id}`);
  }

  create(data: CreateUnitOfMeasureRequest): Observable<ApiResponse<UnitOfMeasureDto>> {
    return this.http.post<ApiResponse<UnitOfMeasureDto>>(this.base, data);
  }

  update(id: number, data: UpdateUnitOfMeasureRequest): Observable<ApiResponse<UnitOfMeasureDto>> {
    return this.http.put<ApiResponse<UnitOfMeasureDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
