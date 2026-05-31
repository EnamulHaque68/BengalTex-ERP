import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  MachineDto, SaveMachineRequest,
  JobCardDto, JobCardListItemDto, CreateJobCardRequest, UpdateJobCardRequest,
  ScanJobCardRequest, JobCardBoardCountsDto, JobCardStatus
} from '../models/job-card.models';

@Injectable({ providedIn: 'root' })
export class JobCardService {
  private readonly machines = `${environment.apiBaseUrl}/api/machines`;
  private readonly jobCards = `${environment.apiBaseUrl}/api/job-cards`;

  constructor(private http: HttpClient) {}

  // ── Machines ──
  getMachines(includeInactive = false): Observable<ApiResponse<MachineDto[]>> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<MachineDto[]>>(this.machines, { params });
  }
  createMachine(data: SaveMachineRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.machines, data);
  }
  updateMachine(id: number, data: SaveMachineRequest): Observable<ApiResponse<number>> {
    return this.http.put<ApiResponse<number>>(`${this.machines}/${id}`, data);
  }
  deleteMachine(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.machines}/${id}`);
  }

  // ── Job Cards ──
  getAll(parameters: PagedQueryParameters, status?: JobCardStatus | null, productionOrderId?: number,
         machineId?: number, operatorEmployeeId?: number, fromDate?: string, toDate?: string)
    : Observable<ApiResponse<PagedResult<JobCardListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (status) params = params.set('status', status);
    if (productionOrderId) params = params.set('productionOrderId', productionOrderId.toString());
    if (machineId) params = params.set('machineId', machineId.toString());
    if (operatorEmployeeId) params = params.set('operatorEmployeeId', operatorEmployeeId.toString());
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<PagedResult<JobCardListItemDto>>>(this.jobCards, { params });
  }
  boardCounts(): Observable<ApiResponse<JobCardBoardCountsDto>> {
    return this.http.get<ApiResponse<JobCardBoardCountsDto>>(`${this.jobCards}/board-counts`);
  }
  getById(id: number): Observable<ApiResponse<JobCardDto>> {
    return this.http.get<ApiResponse<JobCardDto>>(`${this.jobCards}/${id}`);
  }
  create(data: CreateJobCardRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.jobCards, data);
  }
  update(id: number, data: UpdateJobCardRequest): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.jobCards}/${id}`, data);
  }
  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.jobCards}/${id}`);
  }
  scan(data: ScanJobCardRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.jobCards}/scan`, data);
  }
  qrUrl(id: number): string {
    return `${this.jobCards}/${id}/qr`;
  }
}
