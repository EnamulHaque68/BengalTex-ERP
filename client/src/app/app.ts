import { ChangeDetectorRef, Component, NgZone } from '@angular/core';
import { Health, HealthResponse } from './services/health';

@Component({
  selector: 'app-root',
  standalone: false,
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected title = 'Bengal TEX ERP';
  apiStatus = 'Not tested yet';
  apiConnected = false;
  apiError = false;
  loading = false;
  apiResponse: HealthResponse | null = null;
  errorMessage = '';

  constructor(
    private healthService: Health,
    private cdr: ChangeDetectorRef,
    private zone: NgZone
  ) {}

  testApiConnection(): void {
    this.loading = true;
    this.apiError = false;
    this.apiConnected = false;
    this.apiResponse = null;
    this.errorMessage = '';
    this.apiStatus = 'Connecting...';
    this.cdr.detectChanges();

    this.healthService.checkHealth().subscribe({
      next: (response) => {
        this.zone.run(() => {
          this.apiResponse = response;
          this.apiConnected = true;
          this.apiStatus = `✅ Connected! Status: ${response.status}`;
          this.loading = false;
          this.cdr.detectChanges();
          console.log('API Response:', response);
        });
      },
      error: (error) => {
        this.zone.run(() => {
          this.apiError = true;
          this.apiStatus = '❌ Connection failed';
          this.errorMessage = error.message || 'Failed to connect to API';
          this.loading = false;
          this.cdr.detectChanges();
          console.error('API Error:', error);
        });
      }
    });
  }
}