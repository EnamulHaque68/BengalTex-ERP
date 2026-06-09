import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  BankStatementListItemDto, BankStatementDto,
  UnmatchedJournalLineDto,
  CreateBankStatementRequest, UpdateBankStatementRequest, SaveStatementLineRequest,
  ImportBankStatementRequest
} from '../models/bank-reconciliation.models';

@Injectable({ providedIn: 'root' })
export class BankReconciliationService {
  private readonly base = `${environment.apiBaseUrl}/api/bank-statements`;

  constructor(private http: HttpClient) {}

  getAll(parameters: PagedQueryParameters, bankAccountId?: number, isReconciled?: boolean | null)
    : Observable<ApiResponse<PagedResult<BankStatementListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (bankAccountId) params = params.set('bankAccountId', bankAccountId.toString());
    if (isReconciled != null) params = params.set('isReconciled', isReconciled.toString());
    return this.http.get<ApiResponse<PagedResult<BankStatementListItemDto>>>(this.base, { params });
  }
  getById(id: number): Observable<ApiResponse<BankStatementDto>> {
    return this.http.get<ApiResponse<BankStatementDto>>(`${this.base}/${id}`);
  }
  getUnmatchedJournalLines(id: number): Observable<ApiResponse<UnmatchedJournalLineDto[]>> {
    return this.http.get<ApiResponse<UnmatchedJournalLineDto[]>>(`${this.base}/${id}/unmatched-journal-lines`);
  }
  create(data: CreateBankStatementRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }
  importCsv(data: ImportBankStatementRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/import-csv`, data);
  }
  update(id: number, data: UpdateBankStatementRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/${id}`, data);
  }
  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
  reconcile(id: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/${id}/reconcile`, {});
  }

  // ── Lines ──
  addLine(statementId: number, data: SaveStatementLineRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/${statementId}/lines`, data);
  }
  updateLine(lineId: number, data: SaveStatementLineRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.base}/lines/${lineId}`, data);
  }
  deleteLine(lineId: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/lines/${lineId}`);
  }
  match(lineId: number, journalLineId: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/lines/${lineId}/match`, { journalLineId });
  }
  unmatch(lineId: number): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/lines/${lineId}/unmatch`, {});
  }
  exclude(lineId: number, notes: string | null): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.base}/lines/${lineId}/exclude`, { notes });
  }
}
