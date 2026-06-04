import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  FixedAssetDto, SaveFixedAssetRequest, RunDepreciationRequest,
  DisposeFixedAssetRequest, AssetDepreciationRunDto
} from '../models/fixed-asset.models';

@Injectable({ providedIn: 'root' })
export class FixedAssetService {
  private readonly base = `${environment.apiBaseUrl}/api/fixed-assets`;

  constructor(private http: HttpClient) {}

  getAll(p: PagedQueryParameters, status?: string, category?: string)
    : Observable<ApiResponse<PagedResult<FixedAssetDto>>> {
    let params = new HttpParams();
    if (p.page) params = params.set('Page', p.page.toString());
    if (p.pageSize) params = params.set('PageSize', p.pageSize.toString());
    if (p.search) params = params.set('Search', p.search);
    if (status) params = params.set('status', status);
    if (category) params = params.set('category', category);
    return this.http.get<ApiResponse<PagedResult<FixedAssetDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<FixedAssetDto>> {
    return this.http.get<ApiResponse<FixedAssetDto>>(`${this.base}/${id}`);
  }

  create(data: SaveFixedAssetRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  update(id: number, data: SaveFixedAssetRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  runDepreciation(data: RunDepreciationRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/run-depreciation`, data);
  }

  dispose(id: number, data: DisposeFixedAssetRequest): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/dispose`, data);
  }

  getRuns(p: PagedQueryParameters): Observable<ApiResponse<PagedResult<AssetDepreciationRunDto>>> {
    let params = new HttpParams();
    if (p.page) params = params.set('Page', p.page.toString());
    if (p.pageSize) params = params.set('PageSize', p.pageSize.toString());
    return this.http.get<ApiResponse<PagedResult<AssetDepreciationRunDto>>>(`${this.base}/depreciation-runs`, { params });
  }

  getRunById(id: number): Observable<ApiResponse<AssetDepreciationRunDto>> {
    return this.http.get<ApiResponse<AssetDepreciationRunDto>>(`${this.base}/depreciation-runs/${id}`);
  }
}
