import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { PagedQueryParameters, PagedResult } from '../models/user.models';
import {
  CreateEmployeeRequest,
  EmployeeDto,
  EmployeeListItemDto,
  UpdateEmployeeRequest,
  EmployeeHistoryEntryDto,
  AddEmployeeHistoryRequest
} from '../models/employee.models';
import {
  EmployeeProfileDto, UpdateEmployeeProfileRequest, ProfileSkillDto, EmployeeSkillRequest,
  ProfileEducationDto, ProfileEmergencyContactDto, SaveEducationRequest, SaveContactRequest,
  ProfileActivityDto
} from '../models/employee-profile.models';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly base = `${environment.apiBaseUrl}/api/employees`;

  constructor(private http: HttpClient) {}

  getAll(
    parameters: PagedQueryParameters,
    includeInactive = false,
    department?: string,
    status?: string
  ): Observable<ApiResponse<PagedResult<EmployeeListItemDto>>> {
    let params = new HttpParams().set('includeInactive', includeInactive.toString());
    if (parameters.page) params = params.set('Page', parameters.page.toString());
    if (parameters.pageSize) params = params.set('PageSize', parameters.pageSize.toString());
    if (parameters.search) params = params.set('Search', parameters.search);
    if (department) params = params.set('department', department);
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<EmployeeListItemDto>>>(this.base, { params });
  }

  getById(id: number): Observable<ApiResponse<EmployeeDto>> {
    return this.http.get<ApiResponse<EmployeeDto>>(`${this.base}/${id}`);
  }

  // ── Service record (increments / promotions / transfers / disciplinary) ──
  getHistory(id: number): Observable<ApiResponse<EmployeeHistoryEntryDto[]>> {
    return this.http.get<ApiResponse<EmployeeHistoryEntryDto[]>>(`${this.base}/${id}/history`);
  }
  addHistory(id: number, data: AddEmployeeHistoryRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/${id}/history`, data);
  }
  deleteHistory(historyId: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/history/${historyId}`);
  }

  create(data: CreateEmployeeRequest): Observable<ApiResponse<EmployeeDto>> {
    return this.http.post<ApiResponse<EmployeeDto>>(this.base, data);
  }

  update(id: number, data: UpdateEmployeeRequest): Observable<ApiResponse<EmployeeDto>> {
    return this.http.put<ApiResponse<EmployeeDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }

  // ── Profile ──
  getProfile(id: number): Observable<ApiResponse<EmployeeProfileDto>> {
    return this.http.get<ApiResponse<EmployeeProfileDto>>(`${this.base}/${id}/profile`);
  }
  getMyProfile(): Observable<ApiResponse<EmployeeProfileDto>> {
    return this.http.get<ApiResponse<EmployeeProfileDto>>(`${this.base}/my-profile`);
  }
  updateProfile(id: number, data: UpdateEmployeeProfileRequest): Observable<ApiResponse<number>> {
    return this.http.put<ApiResponse<number>>(`${this.base}/${id}/profile`, data);
  }

  // ── Skills ──
  getSkills(id: number): Observable<ApiResponse<ProfileSkillDto[]>> {
    return this.http.get<ApiResponse<ProfileSkillDto[]>>(`${this.base}/${id}/skills`);
  }
  addSkill(id: number, data: EmployeeSkillRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/${id}/skills`, data);
  }
  updateSkill(skillId: number, data: EmployeeSkillRequest): Observable<ApiResponse<number>> {
    return this.http.put<ApiResponse<number>>(`${this.base}/skills/${skillId}`, data);
  }
  deleteSkill(skillId: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/skills/${skillId}`);
  }

  // ── Education ──
  getEducation(id: number): Observable<ApiResponse<ProfileEducationDto[]>> {
    return this.http.get<ApiResponse<ProfileEducationDto[]>>(`${this.base}/${id}/education`);
  }
  saveEducation(id: number, data: SaveEducationRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/${id}/education`, data);
  }
  deleteEducation(eduId: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/education/${eduId}`);
  }

  // ── Emergency contacts ──
  getContacts(id: number): Observable<ApiResponse<ProfileEmergencyContactDto[]>> {
    return this.http.get<ApiResponse<ProfileEmergencyContactDto[]>>(`${this.base}/${id}/contacts`);
  }
  saveContact(id: number, data: SaveContactRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/${id}/contacts`, data);
  }
  deleteContact(contactId: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/contacts/${contactId}`);
  }

  // ── Activity log ──
  getActivity(id: number, page = 1, pageSize = 30): Observable<ApiResponse<PagedResult<ProfileActivityDto>>> {
    const params = new HttpParams().set('Page', page.toString()).set('PageSize', pageSize.toString());
    return this.http.get<ApiResponse<PagedResult<ProfileActivityDto>>>(`${this.base}/${id}/activity`, { params });
  }

  // ── ID card: QR + photo (blob; endpoints are auth'd so fetched via HttpClient) ──
  getQrBlob(id: number): Observable<Blob> {
    return this.http.get(`${this.base}/${id}/qr`, { responseType: 'blob' });
  }
  getPhotoBlob(id: number): Observable<Blob> {
    return this.http.get(`${this.base}/${id}/photo`, { responseType: 'blob' });
  }
  uploadPhoto(id: number, file: File): Observable<ApiResponse<string>> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<ApiResponse<string>>(`${this.base}/${id}/photo`, fd);
  }
}
