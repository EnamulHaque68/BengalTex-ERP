import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  CreateCurrencyRequest,
  CurrencyDto,
  UpdateCurrencyRequest
} from '../models/master-data.models';

@Injectable({ providedIn: 'root' })
export class CurrencyService {
  private readonly base = `${environment.apiBaseUrl}/api/currencies`;

  constructor(private http: HttpClient) {}

  getAll(includeInactive = false): Observable<ApiResponse<CurrencyDto[]>> {
    return this.http.get<ApiResponse<CurrencyDto[]>>(this.base, {
      params: new HttpParams().set('includeInactive', includeInactive.toString())
    });
  }

  getById(id: number): Observable<ApiResponse<CurrencyDto>> {
    return this.http.get<ApiResponse<CurrencyDto>>(`${this.base}/${id}`);
  }

  create(data: CreateCurrencyRequest): Observable<ApiResponse<CurrencyDto>> {
    return this.http.post<ApiResponse<CurrencyDto>>(this.base, data);
  }

  update(id: number, data: UpdateCurrencyRequest): Observable<ApiResponse<CurrencyDto>> {
    return this.http.put<ApiResponse<CurrencyDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
