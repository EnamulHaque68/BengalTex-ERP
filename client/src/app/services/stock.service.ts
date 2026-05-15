import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import { StockOnHandDto, StockMovementDto } from '../models/inventory.models';

@Injectable({ providedIn: 'root' })
export class StockService {
  private readonly base = `${environment.apiBaseUrl}/api/stock`;

  constructor(private http: HttpClient) {}

  getOnHand(
    parameters: PagedQueryParameters,
    warehouseId?: number,
    rawMaterialId?: number,
    belowMinimumOnly = false
  ): Observable<ApiResponse<PagedResult<StockOnHandDto>>> {
    let params = new HttpParams().set('belowMinimumOnly', belowMinimumOnly.toString());
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (warehouseId) params = params.set('warehouseId', warehouseId.toString());
    if (rawMaterialId) params = params.set('rawMaterialId', rawMaterialId.toString());
    return this.http.get<ApiResponse<PagedResult<StockOnHandDto>>>(`${this.base}/on-hand`, { params });
  }

  getMovements(
    parameters: PagedQueryParameters,
    warehouseId?: number,
    rawMaterialId?: number,
    movementType?: string,
    referenceType?: string,
    referenceId?: number
  ): Observable<ApiResponse<PagedResult<StockMovementDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (warehouseId) params = params.set('warehouseId', warehouseId.toString());
    if (rawMaterialId) params = params.set('rawMaterialId', rawMaterialId.toString());
    if (movementType) params = params.set('movementType', movementType);
    if (referenceType) params = params.set('referenceType', referenceType);
    if (referenceId) params = params.set('referenceId', referenceId.toString());
    return this.http.get<ApiResponse<PagedResult<StockMovementDto>>>(`${this.base}/movements`, { params });
  }
}
