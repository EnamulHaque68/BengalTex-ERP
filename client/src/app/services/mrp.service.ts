import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { MrpResultDto } from '../models/mrp.models';

@Injectable({ providedIn: 'root' })
export class MrpService {
  private readonly base = `${environment.apiBaseUrl}/api/mrp`;

  constructor(private http: HttpClient) {}

  getMrp(shortageOnly = false): Observable<ApiResponse<MrpResultDto>> {
    let params = new HttpParams();
    if (shortageOnly) params = params.set('shortageOnly', 'true');
    return this.http.get<ApiResponse<MrpResultDto>>(this.base, { params });
  }

  generateRequisition(rawMaterialIds: number[]): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/generate-requisition`, { rawMaterialIds });
  }
}
