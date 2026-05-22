import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import { StockLotDto, StockLotDetailDto } from '../models/stock-lot.models';

@Injectable({ providedIn: 'root' })
export class StockLotService {
  private readonly base = `${environment.apiBaseUrl}/api/stock-lots`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    itemType?: string,
    warehouseId?: number,
    supplierId?: number,
    status?: string,
    expiringWithinDays?: number,
    activeOnly?: boolean
  ): Observable<ApiResponse<PagedResult<StockLotDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (itemType) params = params.set('itemType', itemType);
    if (warehouseId) params = params.set('warehouseId', warehouseId.toString());
    if (supplierId) params = params.set('supplierId', supplierId.toString());
    if (status) params = params.set('status', status);
    if (expiringWithinDays != null) params = params.set('expiringWithinDays', expiringWithinDays.toString());
    if (activeOnly) params = params.set('activeOnly', 'true');
    return this.http.get<ApiResponse<PagedResult<StockLotDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<StockLotDetailDto>> {
    return this.http.get<ApiResponse<StockLotDetailDto>>(`${this.base}/${id}`);
  }
}
