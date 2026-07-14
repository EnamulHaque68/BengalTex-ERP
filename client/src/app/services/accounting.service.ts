import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  AccountDto, CreateAccountRequest, UpdateAccountRequest,
  JournalEntryDto, JournalEntryListItemDto, SaveJournalEntryRequest,
  TrialBalanceDto, GeneralLedgerDto, ProfitAndLossDto, BalanceSheetDto, CashFlowStatementDto,
  CashBookDto, DayBookDto
} from '../models/accounting.models';

@Injectable({ providedIn: 'root' })
export class AccountingService {
  private readonly accounts = `${environment.apiBaseUrl}/api/accounts`;
  private readonly journals = `${environment.apiBaseUrl}/api/journal-entries`;

  constructor(private http: HttpClient) {}

  // ── Chart of Accounts ──
  getAccounts(accountType?: string, includeInactive = false, postableOnly?: boolean, search?: string)
    : Observable<ApiResponse<AccountDto[]>> {
    let params = new HttpParams().set('includeInactive', includeInactive.toString());
    if (accountType) params = params.set('accountType', accountType);
    if (postableOnly != null) params = params.set('postableOnly', postableOnly.toString());
    if (search) params = params.set('search', search);
    return this.http.get<ApiResponse<AccountDto[]>>(this.accounts, { params });
  }

  createAccount(data: CreateAccountRequest): Observable<ApiResponse<AccountDto>> {
    return this.http.post<ApiResponse<AccountDto>>(this.accounts, data);
  }
  updateAccount(id: number, data: UpdateAccountRequest): Observable<ApiResponse<AccountDto>> {
    return this.http.put<ApiResponse<AccountDto>>(`${this.accounts}/${id}`, data);
  }
  deleteAccount(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.accounts}/${id}`);
  }

  // ── Reports ──
  trialBalance(asOfDate?: string): Observable<ApiResponse<TrialBalanceDto>> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    return this.http.get<ApiResponse<TrialBalanceDto>>(`${this.accounts}/trial-balance`, { params });
  }
  generalLedger(accountId: number, fromDate: string, toDate: string): Observable<ApiResponse<GeneralLedgerDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<GeneralLedgerDto>>(`${this.accounts}/${accountId}/ledger`, { params });
  }
  profitAndLoss(fromDate: string, toDate: string): Observable<ApiResponse<ProfitAndLossDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<ProfitAndLossDto>>(`${this.accounts}/profit-loss`, { params });
  }
  balanceSheet(asOfDate?: string): Observable<ApiResponse<BalanceSheetDto>> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    return this.http.get<ApiResponse<BalanceSheetDto>>(`${this.accounts}/balance-sheet`, { params });
  }
  cashFlow(fromDate: string, toDate: string): Observable<ApiResponse<CashFlowStatementDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<CashFlowStatementDto>>(`${this.accounts}/cash-flow`, { params });
  }

  cashBook(fromDate: string, toDate: string): Observable<ApiResponse<CashBookDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<CashBookDto>>(`${this.accounts}/cash-book`, { params });
  }

  bankBook(fromDate: string, toDate: string, bankAccountId?: number): Observable<ApiResponse<CashBookDto>> {
    let params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    if (bankAccountId) params = params.set('bankAccountId', bankAccountId.toString());
    return this.http.get<ApiResponse<CashBookDto>>(`${this.accounts}/bank-book`, { params });
  }

  dayBook(fromDate: string, toDate: string): Observable<ApiResponse<DayBookDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<DayBookDto>>(`${this.accounts}/day-book`, { params });
  }

  // ── Journal Vouchers ──
  getJournals(parameters: PagedQueryParameters, status?: string, fromDate?: string, toDate?: string, voucherType?: string)
    : Observable<ApiResponse<PagedResult<JournalEntryListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (status) params = params.set('status', status);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    if (voucherType) params = params.set('voucherType', voucherType);
    return this.http.get<ApiResponse<PagedResult<JournalEntryListItemDto>>>(this.journals, { params });
  }
  getJournal(id: number): Observable<ApiResponse<JournalEntryDto>> {
    return this.http.get<ApiResponse<JournalEntryDto>>(`${this.journals}/${id}`);
  }
  createJournal(data: SaveJournalEntryRequest): Observable<ApiResponse<JournalEntryDto>> {
    return this.http.post<ApiResponse<JournalEntryDto>>(this.journals, data);
  }
  updateJournal(id: number, data: SaveJournalEntryRequest): Observable<ApiResponse<JournalEntryDto>> {
    return this.http.put<ApiResponse<JournalEntryDto>>(`${this.journals}/${id}`, data);
  }
  deleteJournal(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.journals}/${id}`);
  }
  postJournal(id: number): Observable<ApiResponse<JournalEntryDto>> {
    return this.http.post<ApiResponse<JournalEntryDto>>(`${this.journals}/${id}/post`, {});
  }

  // ── Phase A1 — contra voucher + reversal ──
  createContra(data: { entryDate: string; fromAccountId: number; toAccountId: number; amount: number; reference: string | null; notes: string | null; })
    : Observable<ApiResponse<JournalEntryDto>> {
    return this.http.post<ApiResponse<JournalEntryDto>>(`${this.journals}/contra`, data);
  }
  reverseJournal(id: number, reason: string, reversalDate?: string | null): Observable<ApiResponse<JournalEntryDto>> {
    return this.http.post<ApiResponse<JournalEntryDto>>(`${this.journals}/${id}/reverse`, { reason, reversalDate: reversalDate ?? null });
  }

  // ── Phase A1 — fiscal years, periods & opening balances ──
  private readonly fiscal = `${environment.apiBaseUrl}/api/financial-years`;

  getFinancialYears(): Observable<ApiResponse<FinancialYearDto[]>> {
    return this.http.get<ApiResponse<FinancialYearDto[]>>(this.fiscal);
  }
  createFinancialYear(data: { code: string; startDate: string; notes: string | null; }): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.fiscal, data);
  }
  changePeriodStatus(periodId: number, action: 'soft-close' | 'lock' | 'reopen'): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.fiscal}/periods/${periodId}/${action}`, {});
  }
  yearClosePreview(id: number): Observable<ApiResponse<YearClosePreviewDto>> {
    return this.http.get<ApiResponse<YearClosePreviewDto>>(`${this.fiscal}/${id}/close-preview`);
  }
  closeYear(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.fiscal}/${id}/close`, {});
  }
  reopenYear(id: number, reason: string): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.fiscal}/${id}/reopen`, { reason });
  }
  openingBalanceTemplate(): Observable<ApiResponse<OpeningBalanceAccountDto[]>> {
    return this.http.get<ApiResponse<OpeningBalanceAccountDto[]>>(`${this.fiscal}/opening-balances/template`);
  }
  importOpeningBalances(asOfDate: string, lines: { accountId: number; debit: number; credit: number; }[])
    : Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.fiscal}/opening-balances/import`, { asOfDate, lines });
  }

  // ── Phase A2 — Inventory ↔ GL (GR/IR init + tie-out) ──
  private readonly inventoryGl = `${environment.apiBaseUrl}/api/inventory-gl`;

  grIrInitPreview(): Observable<ApiResponse<GrIrInitPreviewDto>> {
    return this.http.get<ApiResponse<GrIrInitPreviewDto>>(`${this.inventoryGl}/gr-ir/init-preview`);
  }
  initializeGrIr(asOfDate: string): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.inventoryGl}/gr-ir/initialize`, { asOfDate });
  }
  inventoryGlTieOut(asOfDate?: string): Observable<ApiResponse<InventoryGlTieOutDto>> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    return this.http.get<ApiResponse<InventoryGlTieOutDto>>(`${this.inventoryGl}/tie-out`, { params });
  }

  // ── Phase A3 — cost centers + profitability ──
  private readonly costCenters = `${environment.apiBaseUrl}/api/cost-centers`;
  private readonly reports = `${environment.apiBaseUrl}/api/reports`;

  getCostCenters(includeInactive = false): Observable<ApiResponse<CostCenterDto[]>> {
    let params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<CostCenterDto[]>>(this.costCenters, { params });
  }
  createCostCenter(data: SaveCostCenterRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.costCenters, data);
  }
  updateCostCenter(id: number, data: SaveCostCenterRequest & { isActive: boolean }): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.costCenters}/${id}`, { id, ...data });
  }
  profitability(dimension: 'buyer' | 'style' | 'order', fromDate: string, toDate: string)
    : Observable<ApiResponse<ProfitabilityReportDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<ProfitabilityReportDto>>(`${this.reports}/profitability/${dimension}`, { params });
  }
  costCenterStatement(fromDate: string, toDate: string): Observable<ApiResponse<CostCenterStatementDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<CostCenterStatementDto>>(`${this.reports}/cost-center-statement`, { params });
  }

  // ── Phase A4 — costing rates + production costing + close steps ──
  private readonly costingRates = `${environment.apiBaseUrl}/api/costing-rates`;

  getCostingRates(includeInactive = false): Observable<ApiResponse<CostingRateDto[]>> {
    return this.http.get<ApiResponse<CostingRateDto[]>>(this.costingRates, { params: new HttpParams().set('includeInactive', includeInactive.toString()) });
  }
  createCostingRate(data: SaveCostingRateRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.costingRates, data);
  }
  updateCostingRate(id: number, data: SaveCostingRateRequest & { isActive: boolean }): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.costingRates}/${id}`, { id, ...data });
  }
  productionCostSheet(fromDate: string, toDate: string): Observable<ApiResponse<ProductionCostSheetDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<ProductionCostSheetDto>>(`${this.reports}/production-cost-sheet`, { params });
  }
  wipValuation(asOfDate?: string): Observable<ApiResponse<WipReportDto>> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    return this.http.get<ApiResponse<WipReportDto>>(`${this.reports}/wip-valuation`, { params });
  }
  absorptionPreview(fromDate: string, toDate: string): Observable<ApiResponse<AbsorptionPreviewDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<AbsorptionPreviewDto>>(`${this.inventoryGl}/absorption-preview`, { params });
  }
  postAbsorptionTrueUp(fromDate: string, toDate: string, postDate: string): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.inventoryGl}/absorption-true-up`, { fromDate, toDate, postDate });
  }
  postWipSnapshot(asOfDate: string): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.inventoryGl}/wip-snapshot`, { asOfDate });
  }
  // Phase A7b — month-end FC revaluation
  fxRevaluationPreview(asOfDate: string): Observable<ApiResponse<FxRevaluationPreviewDto>> {
    const params = new HttpParams().set('asOfDate', asOfDate);
    return this.http.get<ApiResponse<FxRevaluationPreviewDto>>(`${this.inventoryGl}/fx-revaluation-preview`, { params });
  }
  postFxRevaluation(asOfDate: string): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.inventoryGl}/fx-revaluation`, { asOfDate });
  }
}

export interface FxRevaluationRowDto {
  kind: string; invoiceCode: string; currencyCode: string;
  outstandingFc: number; bookedRate: number; currentRate: number; deltaBdt: number;
}
export interface FxRevaluationPreviewDto {
  asOfDate: string; rows: FxRevaluationRowDto[];
  arDelta: number; apDelta: number; netUnrealized: number;
}

// ── Phase A4 DTOs ──
export interface CostingRateDto {
  id: number; rateType: string; basis: string; rate: number; effectiveFrom: string;
  workCenterId: number | null; workCenterName: string | null; isActive: boolean; notes: string | null;
}
export interface SaveCostingRateRequest {
  rateType: string; basis: string; rate: number; effectiveFrom: string; workCenterId: number | null; notes: string | null;
}
export interface ProductionCostSheetRowDto {
  productionOrderId: number; code: string; productName: string; styleName: string | null;
  quantity: number; status: string;
  materialCost: number; labourCost: number; machineCost: number; overheadCost: number; subcontractCost: number;
  totalCost: number; unitCost: number;
}
export interface ProductionCostSheetDto {
  fromDate: string; toDate: string; rows: ProductionCostSheetRowDto[];
  totalMaterial: number; totalLabour: number; totalMachine: number; totalOverhead: number; totalSubcontract: number; grandTotal: number;
}
export interface WipReportRowDto {
  productionOrderId: number; code: string; productName: string; styleName: string | null;
  quantity: number; estimatedValue: number; startDate: string | null;
}
export interface WipReportDto {
  rows: WipReportRowDto[]; totalEstimatedValue: number; glWipBalance: number; variance: number;
}
export interface AbsorptionPreviewDto {
  appliedLabour: number; appliedFactoryOverhead: number; appliedTotal: number;
  actualLabour: number; actualFactoryOverhead: number; actualTotal: number;
  variance: number; varianceKind: string;
}
export const COSTING_RATE_TYPES = [
  { label: 'Labour', value: 'Labour' }, { label: 'Machine OH', value: 'MachineOH' }, { label: 'Factory OH', value: 'FactoryOH' }
];
export const COSTING_RATE_BASES = [
  { label: 'Per labour-minute', value: 'PerLabourMinute' }, { label: 'Per machine-hour', value: 'PerMachineHour' }, { label: 'Per unit', value: 'PerUnit' }
];

// ── Phase A3 DTOs ──
export interface CostCenterDto {
  id: number; code: string; name: string; kind: string;
  parentCostCenterId: number | null; parentName: string | null;
  departmentId: number | null; departmentName: string | null;
  factoryId: number | null; factoryName: string | null;
  isActive: boolean; description: string | null;
}
export interface SaveCostCenterRequest {
  code?: string; name: string; kind: string;
  parentCostCenterId: number | null; departmentId: number | null; factoryId: number | null; description: string | null;
}
export interface ProfitabilityRowDto {
  dimensionId: number | null; dimensionName: string;
  revenue: number; cogs: number; grossProfit: number; marginPercent: number;
}
export interface ProfitabilityReportDto {
  fromDate: string; toDate: string; dimension: string;
  rows: ProfitabilityRowDto[]; totalRevenue: number; totalCogs: number; totalGrossProfit: number;
}
export interface CostCenterStatementRowDto {
  costCenterId: number | null; costCenterName: string; income: number; expense: number; net: number;
}
export interface CostCenterStatementDto { fromDate: string; toDate: string; rows: CostCenterStatementRowDto[]; }

export const COST_CENTER_KINDS = [
  { label: 'Cost', value: 'Cost' }, { label: 'Profit', value: 'Profit' }, { label: 'Both', value: 'Both' }
];

// ── Phase A2 DTOs ──
export interface GrIrInitPoRowDto {
  purchaseOrderId: number; purchaseOrderCode: string; supplierName: string;
  receivedValue: number; billedValue: number; unbilledValue: number;
}
export interface GrIrInitPreviewDto {
  alreadyInitialized: boolean; totalUnbilledValue: number; rows: GrIrInitPoRowDto[];
}
export interface TieOutRowDto {
  label: string; accountCode: string; stockValue: number; glBalance: number; variance: number; matches: boolean;
}
export interface OpenGrIrPoRowDto {
  purchaseOrderId: number; purchaseOrderCode: string; supplierName: string; unbilledValue: number;
}
export interface InventoryGlTieOutDto {
  asOfDate: string; rows: TieOutRowDto[]; grIrBalance: number; openGrIr: OpenGrIrPoRowDto[];
}

// ── Phase A1 DTOs (kept here with the service for cohesion) ──
export interface AccountingPeriodDto {
  id: number; periodNumber: number; name: string; startDate: string; endDate: string;
  status: string; statusChangedAt: string | null; statusChangedBy: string | null;
}
export interface FinancialYearDto {
  id: number; code: string; startDate: string; endDate: string; status: string;
  closedAt: string | null; closedBy: string | null; notes: string | null;
  periods: AccountingPeriodDto[];
}
export interface YearClosePreviewDto {
  totalIncome: number; totalExpense: number; netIncome: number; accountCount: number;
}
export interface OpeningBalanceAccountDto {
  accountId: number; code: string; name: string; accountType: string;
  currentDebit: number; currentCredit: number;
}
