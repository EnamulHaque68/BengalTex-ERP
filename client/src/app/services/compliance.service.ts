import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  ComplianceCertificateDto, SaveCertificateRequest, CertificateType, ExpiryStatus,
  ComplianceAuditDto, ComplianceAuditListItemDto, CreateAuditRequest, UpdateAuditRequest,
  AuditType, AuditStatus,
  AddFindingRequest, UpdateFindingRequest,
  ComplianceDashboardDto
} from '../models/compliance.models';

@Injectable({ providedIn: 'root' })
export class ComplianceService {
  private readonly certs = `${environment.apiBaseUrl}/api/compliance-certificates`;
  private readonly audits = `${environment.apiBaseUrl}/api/compliance-audits`;
  private readonly dashboard = `${environment.apiBaseUrl}/api/compliance-dashboard`;

  constructor(private http: HttpClient) {}

  // ── Certificates ──
  getCertificates(parameters: PagedQueryParameters, certificateType?: CertificateType | null,
                  expiryStatus?: ExpiryStatus | null, includeInactive = false)
    : Observable<ApiResponse<PagedResult<ComplianceCertificateDto>>> {
    let params = new HttpParams().set('includeInactive', includeInactive.toString());
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (certificateType) params = params.set('certificateType', certificateType);
    if (expiryStatus) params = params.set('expiryStatus', expiryStatus);
    return this.http.get<ApiResponse<PagedResult<ComplianceCertificateDto>>>(this.certs, { params });
  }
  createCertificate(data: SaveCertificateRequest): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(this.certs, data); }
  updateCertificate(id: number, data: SaveCertificateRequest): Observable<ApiResponse<number>> { return this.http.put<ApiResponse<number>>(`${this.certs}/${id}`, data); }
  deleteCertificate(id: number): Observable<ApiResponse<null>> { return this.http.delete<ApiResponse<null>>(`${this.certs}/${id}`); }

  // ── Audits ──
  getAudits(parameters: PagedQueryParameters, auditType?: AuditType | null,
            status?: AuditStatus | null, fromDate?: string, toDate?: string)
    : Observable<ApiResponse<PagedResult<ComplianceAuditListItemDto>>> {
    let params = new HttpParams();
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (auditType) params = params.set('auditType', auditType);
    if (status) params = params.set('status', status);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<PagedResult<ComplianceAuditListItemDto>>>(this.audits, { params });
  }
  getAuditById(id: number): Observable<ApiResponse<ComplianceAuditDto>> { return this.http.get<ApiResponse<ComplianceAuditDto>>(`${this.audits}/${id}`); }
  createAudit(data: CreateAuditRequest): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(this.audits, data); }
  updateAudit(id: number, data: UpdateAuditRequest): Observable<ApiResponse<null>> { return this.http.put<ApiResponse<null>>(`${this.audits}/${id}`, data); }
  deleteAudit(id: number): Observable<ApiResponse<null>> { return this.http.delete<ApiResponse<null>>(`${this.audits}/${id}`); }

  addFinding(auditId: number, data: AddFindingRequest): Observable<ApiResponse<number>> { return this.http.post<ApiResponse<number>>(`${this.audits}/${auditId}/findings`, data); }
  updateFinding(findingId: number, data: UpdateFindingRequest): Observable<ApiResponse<null>> { return this.http.put<ApiResponse<null>>(`${this.audits}/findings/${findingId}`, data); }
  deleteFinding(findingId: number): Observable<ApiResponse<null>> { return this.http.delete<ApiResponse<null>>(`${this.audits}/findings/${findingId}`); }

  // ── Dashboard ──
  getDashboard(): Observable<ApiResponse<ComplianceDashboardDto>> {
    return this.http.get<ApiResponse<ComplianceDashboardDto>>(this.dashboard);
  }
}
