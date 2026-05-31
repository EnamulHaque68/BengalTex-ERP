import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  AccountDto, CreateAccountRequest, UpdateAccountRequest,
  JournalEntryDto, JournalEntryListItemDto, SaveJournalEntryRequest,
  TrialBalanceDto, GeneralLedgerDto, ProfitAndLossDto, BalanceSheetDto, CashFlowStatementDto
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

  // ── Journal Vouchers ──
  getJournals(parameters: PagedQueryParameters, status?: string, fromDate?: string, toDate?: string)
    : Observable<ApiResponse<PagedResult<JournalEntryListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (status) params = params.set('status', status);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
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
}
