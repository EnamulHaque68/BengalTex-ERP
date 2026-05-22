import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  ExpenseCategoryDto, SaveExpenseCategoryRequest,
  ExpenseDto, ExpenseListItemDto, SaveExpenseRequest, ExpenseSummaryDto
} from '../models/expense.models';

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private readonly expenses = `${environment.apiBaseUrl}/api/expenses`;
  private readonly categories = `${environment.apiBaseUrl}/api/expense-categories`;

  constructor(private http: HttpClient) {}

  // ── Categories ──
  getCategories(includeInactive = false): Observable<ApiResponse<ExpenseCategoryDto[]>> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<ExpenseCategoryDto[]>>(this.categories, { params });
  }
  createCategory(data: SaveExpenseCategoryRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.categories, data);
  }
  updateCategory(id: number, data: SaveExpenseCategoryRequest): Observable<ApiResponse<number>> {
    return this.http.put<ApiResponse<number>>(`${this.categories}/${id}`, data);
  }
  deleteCategory(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.categories}/${id}`);
  }

  // ── Expenses ──
  getAll(parameters: PagedQueryParameters, categoryId?: number, status?: string, fromDate?: string, toDate?: string)
    : Observable<ApiResponse<PagedResult<ExpenseListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (categoryId) params = params.set('categoryId', categoryId.toString());
    if (status) params = params.set('status', status);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<PagedResult<ExpenseListItemDto>>>(this.expenses, { params });
  }
  getById(id: number): Observable<ApiResponse<ExpenseDto>> {
    return this.http.get<ApiResponse<ExpenseDto>>(`${this.expenses}/${id}`);
  }
  create(data: SaveExpenseRequest): Observable<ApiResponse<ExpenseDto>> {
    return this.http.post<ApiResponse<ExpenseDto>>(this.expenses, data);
  }
  update(id: number, data: SaveExpenseRequest): Observable<ApiResponse<ExpenseDto>> {
    return this.http.put<ApiResponse<ExpenseDto>>(`${this.expenses}/${id}`, data);
  }
  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.expenses}/${id}`);
  }
  approve(id: number): Observable<ApiResponse<ExpenseDto>> {
    return this.http.post<ApiResponse<ExpenseDto>>(`${this.expenses}/${id}/approve`, {});
  }
  cancel(id: number): Observable<ApiResponse<ExpenseDto>> {
    return this.http.post<ApiResponse<ExpenseDto>>(`${this.expenses}/${id}/cancel`, {});
  }
  summary(fromDate: string, toDate: string): Observable<ApiResponse<ExpenseSummaryDto>> {
    const params = new HttpParams().set('fromDate', fromDate).set('toDate', toDate);
    return this.http.get<ApiResponse<ExpenseSummaryDto>>(`${this.expenses}/summary`, { params });
  }
}
