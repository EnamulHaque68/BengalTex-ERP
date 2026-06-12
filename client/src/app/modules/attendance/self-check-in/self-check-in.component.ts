import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AttendanceService } from '../../../services/attendance.service';
import { AttendanceRecordDto } from '../../../models/attendance.models';
import { TranslationService } from '../../../shared/i18n/translation.service';
import { Lang } from '../../../shared/i18n/translations';

type GpsState = 'idle' | 'requesting' | 'granted' | 'denied' | 'unavailable' | 'error';

@Component({
  selector: 'app-self-check-in',
  standalone: false,
  templateUrl: './self-check-in.component.html',
  styleUrl: './self-check-in.component.scss'
})
export class SelfCheckInComponent implements OnInit {
  gpsState: GpsState = 'idle';
  gpsError = '';
  latitude: number | null = null;
  longitude: number | null = null;
  gpsAccuracyMeters: number | null = null;

  notes = '';

  checkingIn = false;
  checkInError = '';
  todayRecord: AttendanceRecordDto | null = null;

  constructor(
    private svc: AttendanceService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
    public i18n: TranslationService
  ) {}

  setLang(lang: Lang): void {
    this.i18n.setLang(lang);
    this.cdr.detectChanges();
  }

  ngOnInit(): void {
    // Auto-request GPS on page load — most browsers prompt the user immediately.
    this.requestGps();
  }

  requestGps(): void {
    if (!('geolocation' in navigator)) {
      this.gpsState = 'unavailable';
      this.gpsError = 'Your browser does not support GPS / geolocation.';
      this.cdr.detectChanges();
      return;
    }
    this.gpsState = 'requesting';
    this.gpsError = '';
    this.cdr.detectChanges();

    navigator.geolocation.getCurrentPosition(
      (pos) => this.zone.run(() => {
        this.latitude = pos.coords.latitude;
        this.longitude = pos.coords.longitude;
        this.gpsAccuracyMeters = pos.coords.accuracy;
        this.gpsState = 'granted';
        this.cdr.detectChanges();
      }),
      (err) => this.zone.run(() => {
        this.gpsState = err.code === err.PERMISSION_DENIED ? 'denied' : 'error';
        this.gpsError = err.code === err.PERMISSION_DENIED
          ? 'Location permission denied. You can still check in — but the geo-fence won\'t be validated.'
          : `Could not get your location (${err.message || 'unknown error'}). You can still check in.`;
        this.cdr.detectChanges();
      }),
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 30_000 }
    );
  }

  checkIn(): void {
    if (this.checkingIn || this.todayRecord) return;
    this.checkingIn = true;
    this.checkInError = '';
    this.cdr.detectChanges();

    this.svc.selfCheckIn({
      latitude: this.latitude,
      longitude: this.longitude,
      notes: this.notes?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.checkingIn = false;
        if (res.success && res.data) {
          this.todayRecord = res.data;
        } else {
          this.checkInError = res.message || 'Check-in failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.checkingIn = false;
        this.checkInError = err?.error?.message || 'Check-in failed.';
        this.cdr.detectChanges();
      })
    });
  }

  resetForNext(): void {
    this.todayRecord = null;
    this.notes = '';
    this.checkInError = '';
    this.requestGps();
  }

  formatCoord(c: number | null, suffixPos: string, suffixNeg: string): string {
    if (c == null) return '—';
    const abs = Math.abs(c);
    const suffix = c >= 0 ? suffixPos : suffixNeg;
    return `${abs.toFixed(6)}° ${suffix}`;
  }

  formatDistance(d: number | null): string {
    if (d == null) return '—';
    if (d < 1000) return `${Math.round(d)} m`;
    return `${(d / 1000).toFixed(2)} km`;
  }

  get isInsideFence(): boolean | null {
    return this.todayRecord?.checkInWithinFence ?? null;
  }
}
