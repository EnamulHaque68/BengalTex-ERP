import { ChangeDetectorRef, Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { JobCardService } from '../../../services/job-card.service';
import { AuthService } from '../../../services/auth.service';
import { JobCardDto } from '../../../models/job-card.models';

@Component({
  selector: 'app-job-card-detail',
  standalone: false,
  templateUrl: './job-card-detail.component.html',
  styleUrl: './job-card-detail.component.scss'
})
export class JobCardDetailComponent implements OnInit, OnDestroy {

  loading = false;
  card: JobCardDto | null = null;
  qrUrl: SafeUrl | null = null;
  private qrBlobUrl: string | null = null;

  canScan = false;

  constructor(
    private svc: JobCardService,
    private auth: AuthService,
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private sanitizer: DomSanitizer,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canScan = this.auth.hasPermission('JobCards.Scan');
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.load(id);
  }

  ngOnDestroy(): void {
    if (this.qrBlobUrl) URL.revokeObjectURL(this.qrBlobUrl);
  }

  load(id: number): void {
    this.loading = true;
    this.svc.getById(id).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.card = res.data;
          this.loadQr(id);
        }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  private loadQr(id: number): void {
    this.http.get(this.svc.qrUrl(id), { responseType: 'blob' }).subscribe({
      next: (blob) => this.zone.run(() => {
        if (this.qrBlobUrl) URL.revokeObjectURL(this.qrBlobUrl);
        this.qrBlobUrl = URL.createObjectURL(blob);
        this.qrUrl = this.sanitizer.bypassSecurityTrustUrl(this.qrBlobUrl);
        this.cdr.detectChanges();
      })
    });
  }

  back(): void { this.router.navigate(['/job-cards']); }

  print(): void { window.print(); }

  scanShortcut(scanType: 'Start' | 'Pause' | 'Resume' | 'Complete' | 'Cancel'): void {
    if (!this.card) return;
    this.svc.scan({ jobCardId: this.card.id, scanType }).subscribe({
      next: (res) => this.zone.run(() => { if (res.success) this.load(this.card!.id); })
    });
  }

  statusSeverity(s: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (s) {
      case 'Open': return 'secondary';
      case 'InProgress': return 'success';
      case 'OnHold': return 'warn';
      case 'Completed': return 'info';
      case 'Cancelled': return 'danger';
      default: return 'secondary';
    }
  }

  scanTypeColor(t: string): string {
    switch (t) {
      case 'Start':    return '#059669';
      case 'Pause':    return '#d97706';
      case 'Resume':   return '#2563eb';
      case 'Complete': return '#16a34a';
      case 'QcCheck':  return '#7c3aed';
      case 'Cancel':   return '#dc2626';
      default:         return '#6b7280';
    }
  }

  formatMinutes(m: number | null): string {
    if (m == null) return '—';
    if (m < 60) return `${m}m`;
    const h = Math.floor(m / 60), r = m % 60;
    return `${h}h ${r}m`;
  }
}
