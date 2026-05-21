import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import { AttachmentDto } from '../models/attachment.models';

@Injectable({ providedIn: 'root' })
export class AttachmentService {
  private readonly base = `${environment.apiBaseUrl}/api/attachments`;

  constructor(private http: HttpClient) {}

  list(entityType: string, entityId: number): Observable<ApiResponse<AttachmentDto[]>> {
    const params = new HttpParams()
      .set('entityType', entityType)
      .set('entityId', entityId.toString());
    return this.http.get<ApiResponse<AttachmentDto[]>>(this.base, { params });
  }

  upload(
    entityType: string,
    entityId: number,
    file: File,
    description?: string | null,
    category?: string | null
  ): Observable<ApiResponse<AttachmentDto>> {
    const fd = new FormData();
    fd.append('file', file);
    fd.append('entityType', entityType);
    fd.append('entityId', entityId.toString());
    if (description) fd.append('description', description);
    if (category) fd.append('category', category);
    return this.http.post<ApiResponse<AttachmentDto>>(this.base, fd);
  }

  download(id: number): Observable<Blob> {
    return this.http.get(`${this.base}/${id}/download`, { responseType: 'blob' });
  }

  delete(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.base}/${id}`);
  }
}
