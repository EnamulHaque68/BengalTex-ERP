import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { CompanyDto, FactoryDto, FactoryListItemDto } from '../models/company.models';

@Injectable({ providedIn: 'root' })
export class CompanyService {
  private readonly base = `${environment.apiBaseUrl}/api/company`;

  constructor(private http: HttpClient) {}

  getCompany(): Observable<ApiResponse<CompanyDto>> {
    return this.http.get<ApiResponse<CompanyDto>>(this.base);
  }

  updateCompany(data: Omit<CompanyDto, 'id' | 'isActive'>): Observable<ApiResponse<CompanyDto>> {
    return this.http.put<ApiResponse<CompanyDto>>(this.base, data);
  }

  /** Public logo URL — usable directly in <img src>. Cache-busted via the optional token. */
  logoUrl(cacheBust?: string | number): string {
    return `${this.base}/logo${cacheBust ? `?v=${cacheBust}` : ''}`;
  }

  uploadLogo(file: File): Observable<ApiResponse<CompanyDto>> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ApiResponse<CompanyDto>>(`${this.base}/logo`, form);
  }

  deleteLogo(): Observable<ApiResponse<CompanyDto>> {
    return this.http.delete<ApiResponse<CompanyDto>>(`${this.base}/logo`);
  }
}

@Injectable({ providedIn: 'root' })
export class FactoryService {
  private readonly base = `${environment.apiBaseUrl}/api/factories`;

  constructor(private http: HttpClient) {}

  getAll(includeInactive = false): Observable<ApiResponse<FactoryListItemDto[]>> {
    return this.http.get<ApiResponse<FactoryListItemDto[]>>(this.base, {
      params: { includeInactive: includeInactive.toString() }
    });
  }

  getById(id: number): Observable<ApiResponse<FactoryDto>> {
    return this.http.get<ApiResponse<FactoryDto>>(`${this.base}/${id}`);
  }

  create(data: Omit<FactoryDto, 'id'>): Observable<ApiResponse<FactoryDto>> {
    return this.http.post<ApiResponse<FactoryDto>>(this.base, data);
  }

  update(id: number, data: Omit<FactoryDto, 'id' | 'companyId'>): Observable<ApiResponse<FactoryDto>> {
    return this.http.put<ApiResponse<FactoryDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
