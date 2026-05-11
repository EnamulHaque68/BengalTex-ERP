import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface HealthResponse {
  status: string;
  time: string;
}

@Injectable({
  providedIn: 'root'
})
export class Health {
  private apiUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) { }

  checkHealth(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>(`${this.apiUrl}/health`);
  }

  getRoot(): Observable<string> {
    return this.http.get(`${this.apiUrl}/`, { responseType: 'text' });
  }
}