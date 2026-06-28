import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CompleteProductionStageRequest,
  CreateProductionOrderRequest,
  ProductionAwaitingQcDto,
  ProductionCalendarDto,
  ProductionOrderDto,
  ProductionOrderListItemDto,
  ProductionTraceabilityDto,
  UpdateProductionCostsRequest,
  UpdateProductionOrderRequest
} from '../models/production-order.models';

@Injectable({ providedIn: 'root' })
export class ProductionOrderService {
  private readonly base = `${environment.apiBaseUrl}/api/production-orders`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    productId?: number,
    status?: string,
    salesOrderId?: number
  ): Observable<ApiResponse<PagedResult<ProductionOrderListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (parameters.sortBy) params = params.set('SortBy', parameters.sortBy);
    if (parameters.sortDirection) params = params.set('SortDirection', parameters.sortDirection);
    if (productId) params = params.set('productId', productId.toString());
    if (status) params = params.set('status', status);
    if (salesOrderId) params = params.set('salesOrderId', salesOrderId.toString());
    return this.http.get<ApiResponse<PagedResult<ProductionOrderListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.get<ApiResponse<ProductionOrderDto>>(`${this.base}/${id}`);
  }

  /** Phase 8 — Manufacturing Calendar: orders + holidays + weekend days for a date range. */
  getCalendar(from: string, to: string): Observable<ApiResponse<ProductionCalendarDto>> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<ApiResponse<ProductionCalendarDto>>(`${this.base}/calendar`, { params });
  }

  /** Phase 5b — completed productions still QC-held (remaining hold > 0) for the QC inspection picker. */
  getAwaitingQc(): Observable<ApiResponse<ProductionAwaitingQcDto[]>> {
    return this.http.get<ApiResponse<ProductionAwaitingQcDto[]>>(`${this.base}/awaiting-qc`);
  }

  /** Phase 6 — record the manual cost-sheet components. */
  updateCosts(id: number, data: UpdateProductionCostsRequest): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(`${this.base}/${id}/costs`, data);
  }

  /** Phase 7 — end-to-end traceability chain for a production run. */
  getTraceability(id: number): Observable<ApiResponse<ProductionTraceabilityDto>> {
    return this.http.get<ApiResponse<ProductionTraceabilityDto>>(`${this.base}/${id}/traceability`);
  }

  create(data: CreateProductionOrderRequest): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(this.base, data);
  }

  update(id: number, data: UpdateProductionOrderRequest): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.put<ApiResponse<ProductionOrderDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  start(id: number): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(`${this.base}/${id}/start`, {});
  }

  complete(id: number): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(`${this.base}/${id}/complete`, {});
  }

  cancel(id: number): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(`${this.base}/${id}/cancel`, {});
  }

  /** Phase 5 — release the QC hold on a completed run (makes the held finished goods usable). */
  releaseQcHold(id: number): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(`${this.base}/${id}/release-qc-hold`, {});
  }

  /** Raise a draft Purchase Requisition for this order's raw-material shortfalls (returns the PR id). */
  generatePr(id: number): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${environment.apiBaseUrl}/api/purchase-requisitions/from-production/${id}`, {});
  }

  // ── Routing stage workflow ──
  startStage(stageId: number): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(`${this.base}/stages/${stageId}/start`, {});
  }

  completeStage(stageId: number, data: CompleteProductionStageRequest): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(`${this.base}/stages/${stageId}/complete`, data);
  }

  skipStage(stageId: number): Observable<ApiResponse<ProductionOrderDto>> {
    return this.http.post<ApiResponse<ProductionOrderDto>>(`${this.base}/stages/${stageId}/skip`, {});
  }
}
