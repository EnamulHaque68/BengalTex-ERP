import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { ExchangeRateDto, SetExchangeRateRequest } from '../models/exchange-rate.models';

/** Phase A6c — dated FX rate history. */
@Injectable({ providedIn: 'root' })
export class ExchangeRateService {
  private readonly base = `${environment.apiBaseUrl}/api/exchange-rates`;

  constructor(private http: HttpClient) {}

  getAll(currencyId?: number): Observable<ApiResponse<ExchangeRateDto[]>> {
    let params = new HttpParams();
    if (currencyId) params = params.set('currencyId', currencyId.toString());
    return this.http.get<ApiResponse<ExchangeRateDto[]>>(this.base, { params });
  }

  set(body: SetExchangeRateRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, body);
  }
}
