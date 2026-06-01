import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  DepartmentDto, SaveDepartmentRequest,
  DesignationDto, SaveDesignationRequest,
  ShiftDto, CreateShiftRequest, UpdateShiftRequest,
  BankAccountDto, SaveBankAccountRequest
} from '../models/master-setup.models';

@Injectable({ providedIn: 'root' })
export class MasterSetupService {
  private readonly departments = `${environment.apiBaseUrl}/api/departments`;
  private readonly designations = `${environment.apiBaseUrl}/api/designations`;
  private readonly shifts = `${environment.apiBaseUrl}/api/shifts`;
  private readonly bankAccounts = `${environment.apiBaseUrl}/api/bank-accounts`;

  constructor(private http: HttpClient) {}

  // ── Departments ──
  getDepartments(includeInactive = false): Observable<ApiResponse<DepartmentDto[]>> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<DepartmentDto[]>>(this.departments, { params });
  }
  createDepartment(data: SaveDepartmentRequest): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(this.departments, data); }
  updateDepartment(id: number, data: SaveDepartmentRequest): Observable<ApiResponse<number>> { return this.http.put<ApiResponse<number>>(`${this.departments}/${id}`, data); }
  deleteDepartment(id: number): Observable<ApiResponse<null>> { return this.http.delete<ApiResponse<null>>(`${this.departments}/${id}`); }

  // ── Designations ──
  getDesignations(includeInactive = false): Observable<ApiResponse<DesignationDto[]>> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<DesignationDto[]>>(this.designations, { params });
  }
  createDesignation(data: SaveDesignationRequest): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(this.designations, data); }
  updateDesignation(id: number, data: SaveDesignationRequest): Observable<ApiResponse<number>> { return this.http.put<ApiResponse<number>>(`${this.designations}/${id}`, data); }
  deleteDesignation(id: number): Observable<ApiResponse<null>> { return this.http.delete<ApiResponse<null>>(`${this.designations}/${id}`); }

  // ── Shifts ──
  getShifts(includeInactive = false): Observable<ApiResponse<ShiftDto[]>> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<ShiftDto[]>>(this.shifts, { params });
  }
  createShift(data: CreateShiftRequest): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(this.shifts, data); }
  updateShift(id: number, data: UpdateShiftRequest): Observable<ApiResponse<number>> { return this.http.put<ApiResponse<number>>(`${this.shifts}/${id}`, data); }
  deleteShift(id: number): Observable<ApiResponse<null>> { return this.http.delete<ApiResponse<null>>(`${this.shifts}/${id}`); }

  // ── Bank Accounts ──
  getBankAccounts(includeInactive = false): Observable<ApiResponse<BankAccountDto[]>> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<BankAccountDto[]>>(this.bankAccounts, { params });
  }
  createBankAccount(data: SaveBankAccountRequest): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(this.bankAccounts, data); }
  updateBankAccount(id: number, data: SaveBankAccountRequest): Observable<ApiResponse<number>> { return this.http.put<ApiResponse<number>>(`${this.bankAccounts}/${id}`, data); }
  deleteBankAccount(id: number): Observable<ApiResponse<null>> { return this.http.delete<ApiResponse<null>>(`${this.bankAccounts}/${id}`); }
}
