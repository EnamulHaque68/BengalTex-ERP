import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  ApAgeingReportDto,
  ArAgeingReportDto,
  DashboardKpisDto,
  MarginReportDto,
  SalesSummaryReportDto,
  StockSummaryReportDto,
  StockLedgerReportDto,
  DeadStockReportDto,
  WastageVarianceReportDto,
  VatSummaryReportDto,
  WipReportDto,
  ProductionSummaryReportDto,
  OperatorProductivityReportDto,
  MachineProductivityReportDto,
  BuyerOrderBookReportDto,
  EpbExportRegisterReportDto,
  CustomerStatementReportDto,
  SupplierStatementReportDto
} from '../models/reports.models';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly base = `${environment.apiBaseUrl}/api/reports`;

  constructor(private http: HttpClient) {}

  getStockSummary(itemType?: string, warehouseId?: number): Observable<ApiResponse<StockSummaryReportDto>> {
    let params = new HttpParams();
    if (itemType) params = params.set('itemType', itemType);
    if (warehouseId) params = params.set('warehouseId', warehouseId.toString());
    return this.http.get<ApiResponse<StockSummaryReportDto>>(`${this.base}/stock-summary`, { params });
  }

  getStockLedger(
    itemType: string, itemId: number, warehouseId?: number, fromDate?: string, toDate?: string
  ): Observable<ApiResponse<StockLedgerReportDto>> {
    let params = new HttpParams().set('itemType', itemType).set('itemId', itemId.toString());
    if (warehouseId) params = params.set('warehouseId', warehouseId.toString());
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<StockLedgerReportDto>>(`${this.base}/stock-ledger`, { params });
  }

  getDeadStock(
    daysThreshold = 90, itemType?: string, warehouseId?: number, asOfDate?: string
  ): Observable<ApiResponse<DeadStockReportDto>> {
    let params = new HttpParams().set('daysThreshold', daysThreshold.toString());
    if (itemType) params = params.set('itemType', itemType);
    if (warehouseId) params = params.set('warehouseId', warehouseId.toString());
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    return this.http.get<ApiResponse<DeadStockReportDto>>(`${this.base}/dead-stock`, { params });
  }

  getArAgeing(asOfDate?: string, customerId?: number): Observable<ApiResponse<ArAgeingReportDto>> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    if (customerId) params = params.set('customerId', customerId.toString());
    return this.http.get<ApiResponse<ArAgeingReportDto>>(`${this.base}/ar-ageing`, { params });
  }

  getApAgeing(asOfDate?: string, supplierId?: number): Observable<ApiResponse<ApAgeingReportDto>> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    if (supplierId) params = params.set('supplierId', supplierId.toString());
    return this.http.get<ApiResponse<ApAgeingReportDto>>(`${this.base}/ap-ageing`, { params });
  }

  getSalesSummary(fromDate?: string, toDate?: string, customerId?: number): Observable<ApiResponse<SalesSummaryReportDto>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    if (customerId) params = params.set('customerId', customerId.toString());
    return this.http.get<ApiResponse<SalesSummaryReportDto>>(`${this.base}/sales-summary`, { params });
  }

  getDashboardKpis(): Observable<ApiResponse<DashboardKpisDto>> {
    return this.http.get<ApiResponse<DashboardKpisDto>>(`${this.base}/dashboard-kpis`);
  }

  getVatSummary(fromDate?: string, toDate?: string): Observable<ApiResponse<VatSummaryReportDto>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<VatSummaryReportDto>>(`${this.base}/vat-summary`, { params });
  }

  getMargin(fromDate?: string, toDate?: string, customerId?: number): Observable<ApiResponse<MarginReportDto>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    if (customerId) params = params.set('customerId', customerId.toString());
    return this.http.get<ApiResponse<MarginReportDto>>(`${this.base}/margin`, { params });
  }

  getWip(): Observable<ApiResponse<WipReportDto>> {
    return this.http.get<ApiResponse<WipReportDto>>(`${this.base}/wip`);
  }

  getWastageVariance(fromDate?: string, toDate?: string, productId?: number): Observable<ApiResponse<WastageVarianceReportDto>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    if (productId) params = params.set('productId', productId.toString());
    return this.http.get<ApiResponse<WastageVarianceReportDto>>(`${this.base}/wastage-variance`, { params });
  }

  getProductionSummary(fromDate?: string, toDate?: string, productId?: number): Observable<ApiResponse<ProductionSummaryReportDto>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    if (productId) params = params.set('productId', productId.toString());
    return this.http.get<ApiResponse<ProductionSummaryReportDto>>(`${this.base}/production-summary`, { params });
  }

  getOperatorProductivity(fromDate?: string, toDate?: string): Observable<ApiResponse<OperatorProductivityReportDto>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<OperatorProductivityReportDto>>(`${this.base}/operator-productivity`, { params });
  }

  getMachineProductivity(fromDate?: string, toDate?: string): Observable<ApiResponse<MachineProductivityReportDto>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<MachineProductivityReportDto>>(`${this.base}/machine-productivity`, { params });
  }

  getBuyerOrderBook(customerId?: number): Observable<ApiResponse<BuyerOrderBookReportDto>> {
    let params = new HttpParams();
    if (customerId) params = params.set('customerId', customerId.toString());
    return this.http.get<ApiResponse<BuyerOrderBookReportDto>>(`${this.base}/buyer-order-book`, { params });
  }

  getEpbExportRegister(fromDate?: string, toDate?: string, pendingFormExpOnly = false): Observable<ApiResponse<EpbExportRegisterReportDto>> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    if (pendingFormExpOnly) params = params.set('pendingFormExpOnly', 'true');
    return this.http.get<ApiResponse<EpbExportRegisterReportDto>>(`${this.base}/epb-export-register`, { params });
  }

  getCustomerStatement(customerId: number, fromDate?: string, toDate?: string): Observable<ApiResponse<CustomerStatementReportDto>> {
    let params = new HttpParams().set('customerId', customerId.toString());
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<CustomerStatementReportDto>>(`${this.base}/customer-statement`, { params });
  }

  getSupplierStatement(supplierId: number, fromDate?: string, toDate?: string): Observable<ApiResponse<SupplierStatementReportDto>> {
    let params = new HttpParams().set('supplierId', supplierId.toString());
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<SupplierStatementReportDto>>(`${this.base}/supplier-statement`, { params });
  }

  emailCustomerStatement(data: EmailStatementRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/customer-statement/email`, data);
  }

  emailSupplierStatement(data: EmailStatementRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/supplier-statement/email`, data);
  }
}

export interface EmailStatementRequest {
  partyId: number;                 // customerId or supplierId depending on endpoint
  fromDate: string | null;
  toDate: string | null;
  toAddresses: string;
  ccAddresses: string | null;
  subject: string;
  htmlBody: string;
}
