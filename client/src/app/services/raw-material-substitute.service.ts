import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  RawMaterialSubstituteDto,
  CreateRawMaterialSubstituteRequest,
  UpdateRawMaterialSubstituteRequest
} from '../models/raw-material-substitute.models';

@Injectable({ providedIn: 'root' })
export class RawMaterialSubstituteService {
  private readonly base = `${environment.apiBaseUrl}/api/raw-material-substitutes`;

  constructor(private http: HttpClient) {}

  getForMaterial(rawMaterialId: number): Observable<ApiResponse<RawMaterialSubstituteDto[]>> {
    const params = new HttpParams().set('rawMaterialId', rawMaterialId.toString());
    return this.http.get<ApiResponse<RawMaterialSubstituteDto[]>>(this.base, { params });
  }

  create(data: CreateRawMaterialSubstituteRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  update(id: number, data: UpdateRawMaterialSubstituteRequest): Observable<ApiResponse<number>> {
    return this.http.put<ApiResponse<number>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/${id}`);
  }
}
