import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  WastageReasonDto, SaveWastageReasonRequest,
  WastageEntryDto, WastageEntryListItemDto, SaveWastageEntryRequest, WastageSummaryDto
} from '../models/wastage.models';

@Injectable({ providedIn: 'root' })
export class WastageService {
  private readonly entries = `${environment.apiBaseUrl}/api/wastage-entries`;
  private readonly reasons = `${environment.apiBaseUrl}/api/wastage-reasons`;

  constructor(private http: HttpClient) {}

  // ── Reasons ──
  getReasons(includeInactive = false): Observable<ApiResponse<WastageReasonDto[]>> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<WastageReasonDto[]>>(this.reasons, { params });
  }
  createReason(data: SaveWastageReasonRequest): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(this.reasons, data); }
  updateReason(id: number, data: SaveWastageReasonRequest): Observable<ApiResponse<number>> { return this.http.put<ApiResponse<number>>(`${this.reasons}/${id}`, data); }
  deleteReason(id: number): Observable<ApiResponse<null>> { return this.http.delete<ApiResponse<null>>(`${this.reasons}/${id}`); }

  // ── Entries ──
  getAll(parameters: PagedQueryParameters, rawMaterialId?: number, wastageReasonId?: number, fromDate?: string, toDate?: string)
    : Observable<ApiResponse<PagedResult<WastageEntryListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (rawMaterialId) params = params.set('rawMaterialId', rawMaterialId.toString());
    if (wastageReasonId) params = params.set('wastageReasonId', wastageReasonId.toString());
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<PagedResult<WastageEntryListItemDto>>>(this.entries, { params });
  }
  getById(id: number): Observable<ApiResponse<WastageEntryDto>> { return this.http.get<ApiResponse<WastageEntryDto>>(`${this.entries}/${id}`); }
  create(data: SaveWastageEntryRequest): Observable<ApiResponse<WastageEntryDto>> { return this.http.post<ApiResponse<WastageEntryDto>>(this.entries, data); }
  update(id: number, data: SaveWastageEntryRequest): Observable<ApiResponse<WastageEntryDto>> { return this.http.put<ApiResponse<WastageEntryDto>>(`${this.entries}/${id}`, data); }
  delete(id: number): Observable<ApiResponse<null>> { return this.http.delete<ApiResponse<null>>(`${this.entries}/${id}`); }
  summary(fromDate: string, toDate: string): Observable<ApiResponse<WastageSummaryDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<WastageSummaryDto>>(`${this.entries}/summary`, { params });
  }
}
