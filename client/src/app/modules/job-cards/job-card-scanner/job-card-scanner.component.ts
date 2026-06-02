import { ChangeDetectorRef, Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Html5Qrcode, Html5QrcodeCameraScanConfig } from 'html5-qrcode';
import { JobCardService } from '../../../services/job-card.service';
import { AuthService } from '../../../services/auth.service';

type ScanActionValue = 'Start' | 'Pause' | 'Resume' | 'Complete' | 'QcCheck' | 'Cancel';

interface ScanHistoryEntry {
  at: Date;
  code: string;
  action: ScanActionValue;
  success: boolean;
  message: string;
}

@Component({
  selector: 'app-job-card-scanner',
  standalone: false,
  templateUrl: './job-card-scanner.component.html',
  styleUrl: './job-card-scanner.component.scss'
})
export class JobCardScannerComponent implements OnInit, OnDestroy {

  readonly scannerId = 'qr-scanner-viewport';
  private scanner: Html5Qrcode | null = null;
  cameraActive = false;
  cameraError = '';
  selectedAction: ScanActionValue = 'Start';
  history: ScanHistoryEntry[] = [];
  scanning = false;
  lastScannedAt = 0;
  // Cooldown between same-code re-scans (camera fires continuously)
  private static readonly RescanCooldownMs = 2000;
  private lastCode = '';

  readonly actions: { label: string; value: ScanActionValue; severity: string }[] = [
    { label: 'Start',    value: 'Start',    severity: 'success'   },
    { label: 'Pause',    value: 'Pause',    severity: 'warning'   },
    { label: 'Resume',   value: 'Resume',   severity: 'info'      },
    { label: 'Complete', value: 'Complete', severity: 'success'   },
    { label: 'QC Check', value: 'QcCheck',  severity: 'secondary' },
    { label: 'Cancel',   value: 'Cancel',   severity: 'danger'    }
  ];

  canScan = false;

  constructor(
    private svc: JobCardService,
    private auth: AuthService,
    private router: Router,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canScan = this.auth.hasPermission('JobCards.Scan');
  }

  ngOnDestroy(): void { this.stopScanner(); }

  async startScanner(): Promise<void> {
    if (this.cameraActive || !this.canScan) return;
    this.cameraError = '';
    try {
      this.scanner = new Html5Qrcode(this.scannerId, { verbose: false });
      const config: Html5QrcodeCameraScanConfig = {
        fps: 10,
        qrbox: { width: 280, height: 280 },
        aspectRatio: 1.0
      };
      await this.scanner.start(
        { facingMode: 'environment' },     // prefer back camera on mobile
        config,
        (decoded) => this.onScanSuccess(decoded),
        () => { /* per-frame failures are normal — ignore */ }
      );
      this.cameraActive = true;
      this.cdr.detectChanges();
    } catch (err: any) {
      this.cameraError = err?.message || 'Unable to start camera. Check permissions.';
      this.cdr.detectChanges();
    }
  }

  async stopScanner(): Promise<void> {
    if (!this.scanner) return;
    try { if (this.cameraActive) await this.scanner.stop(); } catch { /* ignore */ }
    try { this.scanner.clear(); } catch { /* ignore */ }
    this.scanner = null;
    this.cameraActive = false;
  }

  private onScanSuccess(decoded: string): void {
    const now = Date.now();
    // Debounce: same code within cooldown is ignored (camera fires every frame)
    if (decoded === this.lastCode && now - this.lastScannedAt < JobCardScannerComponent.RescanCooldownMs) return;
    this.lastCode = decoded;
    this.lastScannedAt = now;

    const code = decoded.trim().toUpperCase();
    const action = this.selectedAction;
    this.scanning = true;
    this.cdr.detectChanges();

    this.svc.scan({ code, scanType: action }).subscribe({
      next: (res) => this.zone.run(() => {
        this.scanning = false;
        this.history.unshift({
          at: new Date(),
          code, action,
          success: !!res.success,
          message: res.message || (res.success ? 'OK' : 'Failed')
        });
        if (this.history.length > 20) this.history.length = 20;
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => {
        this.scanning = false;
        this.history.unshift({
          at: new Date(),
          code, action,
          success: false,
          message: e?.error?.message || 'Scan failed.'
        });
        if (this.history.length > 20) this.history.length = 20;
        this.cdr.detectChanges();
      })
    });
  }

  clearHistory(): void { this.history = []; }
  back(): void { this.router.navigate(['/job-cards']); }

  actionSeverity(s: string): string {
    return this.actions.find(a => a.value === s)?.severity || 'secondary';
  }
}
