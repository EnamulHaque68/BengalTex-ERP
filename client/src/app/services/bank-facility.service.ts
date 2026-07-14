import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  BankFacilityDto, BankFacilityDetailDto, CreateBankFacilityRequest, AddBankFacilityEventRequest
} from '../models/bank-facility.models';

/** Phase A6c — bank treasury facilities (loan / OD / FDR) + their financial events. */
@Injectable({ providedIn: 'root' })
export class BankFacilityService {
  private readonly base = `${environment.apiBaseUrl}/api/bank-facilities`;

  constructor(private http: HttpClient) {}

  getAll(status?: string): Observable<ApiResponse<BankFacilityDto[]>> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<BankFacilityDto[]>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<BankFacilityDetailDto>> {
    return this.http.get<ApiResponse<BankFacilityDetailDto>>(`${this.base}/${id}`);
  }

  create(body: CreateBankFacilityRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, body);
  }

  addEvent(id: number, body: AddBankFacilityEventRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/${id}/events`, body);
  }
}
