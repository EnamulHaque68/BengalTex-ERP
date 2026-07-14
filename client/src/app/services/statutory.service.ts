import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  StatutoryLiabilitiesDto, StatutoryRemittanceDto, PostStatutoryRemittanceRequest
} from '../models/statutory.models';

/** Phase A5b — outstanding AIT/VDS/PF payables + the remittance (challan) register. */
@Injectable({ providedIn: 'root' })
export class StatutoryService {
  private readonly base = `${environment.apiBaseUrl}/api/statutory`;

  constructor(private http: HttpClient) {}

  liabilities(asOfDate?: string): Observable<ApiResponse<StatutoryLiabilitiesDto>> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    return this.http.get<ApiResponse<StatutoryLiabilitiesDto>>(`${this.base}/liabilities`, { params });
  }

  remittances(taxType?: string): Observable<ApiResponse<StatutoryRemittanceDto[]>> {
    let params = new HttpParams();
    if (taxType) params = params.set('taxType', taxType);
    return this.http.get<ApiResponse<StatutoryRemittanceDto[]>>(`${this.base}/remittances`, { params });
  }

  remit(body: PostStatutoryRemittanceRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/remittances`, body);
  }
}
