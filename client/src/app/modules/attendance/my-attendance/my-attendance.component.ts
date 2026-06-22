import { ChangeDetectorRef, Component, ElementRef, NgZone, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { AttendanceService } from '../../../services/attendance.service';
import { AuthService } from '../../../services/auth.service';
import {
  MyAttendanceDto, AttendanceRequestDto, ATTENDANCE_REQUEST_TYPES, TeamAttendanceDto
} from '../../../models/attendance.models';

type GpsState = 'idle' | 'requesting' | 'granted' | 'denied' | 'unavailable' | 'error';
type SelfieFor = 'in' | 'out';

@Component({
  selector: 'app-my-attendance',
  standalone: false,
  templateUrl: './my-attendance.component.html',
  styleUrl: './my-attendance.component.scss'
})
export class MyAttendanceComponent implements OnInit, OnDestroy {
  @ViewChild('video') videoRef?: ElementRef<HTMLVideoElement>;
  @ViewChild('canvas') canvasRef?: ElementRef<HTMLCanvasElement>;

  loading = true;
  loadError = '';
  data: MyAttendanceDto | null = null;

  // GPS
  gpsState: GpsState = 'idle';
  gpsError = '';
  latitude: number | null = null;
  longitude: number | null = null;
  gpsAccuracyMeters: number | null = null;

  // Action state
  busy = false;
  actionError = '';

  // Selfie capture
  selfieOpen = false;
  selfieFor: SelfieFor = 'in';
  cameraReady = false;
  cameraError = '';
  capturedSelfie: string | null = null;          // data URL preview
  private mediaStream: MediaStream | null = null;

  // Live working-hours ticker
  liveLabel = '00h 00m';
  private ticker: any = null;
  private refresher: any = null;

  private lastMapKey = '';
  private mapUrlCache: SafeResourceUrl | null = null;

  // Correction requests
  requestTypes = ATTENDANCE_REQUEST_TYPES;
  myRequests: AttendanceRequestDto[] = [];
  reqOpen = false;
  reqBusy = false;
  reqError = '';
  reqForm = { requestDate: '', requestType: 'TimeCorrection', requestedCheckInTime: '', requestedCheckOutTime: '', reason: '' };

  // Role-wise: team panel (supervisors only)
  canSupervise = false;
  team: TeamAttendanceDto | null = null;

  constructor(
    private svc: AttendanceService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
    private sanitizer: DomSanitizer,
    private auth: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.canSupervise = this.auth.hasPermission('Attendance.ApproveFlagged');
    this.requestGps();
    this.load();
    this.loadRequests();
    if (this.canSupervise) this.loadTeam();
    // Refresh server snapshot every 60s; tick the live label every second.
    this.refresher = setInterval(() => this.zone.run(() => this.load(true)), 60_000);
    this.ticker = setInterval(() => this.zone.run(() => this.tickLive()), 1_000);
  }

  // ── Role-wise team panel ──
  loadTeam(): void {
    const today = new Date().toISOString().slice(0, 10);
    this.svc.getTeamAttendance(today, today, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.team = res.data; this.cdr.detectChanges(); }),
      error: () => {}
    });
  }

  private countTeam(pred: (s: string) => boolean): number {
    return (this.team?.rows ?? []).filter(r => pred(r.status)).length;
  }
  get teamPresent(): number { return this.countTeam(s => ['Present', 'OnTime', 'Late', 'HalfDay', 'Overtime', 'OffdayWork', 'HolidayWork', 'EarlyLeave'].includes(s)); }
  get teamLate(): number { return (this.team?.rows ?? []).filter(r => r.isLate).length; }
  get teamAbsent(): number { return this.countTeam(s => s === 'Absent'); }
  get teamLeave(): number { return this.countTeam(s => s === 'Leave'); }

  goTeam(): void { this.router.navigate(['/attendance/team']); }

  // ── Circular progress ring (worked vs 8h target) ──
  get ringPercent(): number {
    const mins = this.today?.workedMinutes ?? 0;
    return Math.max(0, Math.min(100, Math.round((mins / 480) * 100)));
  }
  get ringDash(): string {
    const c = 2 * Math.PI * 52; // r=52
    const filled = (this.ringPercent / 100) * c;
    return `${filled} ${c - filled}`;
  }

  ngOnDestroy(): void {
    if (this.ticker) clearInterval(this.ticker);
    if (this.refresher) clearInterval(this.refresher);
    this.stopCamera();
  }

  // ── Load ──────────────────────────────────────────────────────────────────
  load(silent = false): void {
    if (!silent) { this.loading = true; this.cdr.detectChanges(); }
    this.svc.getMyAttendance().subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.data = res.data; this.loadError = ''; this.tickLive(); }
        else { this.loadError = res.message || 'Could not load your attendance.'; }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loading = false;
        this.loadError = err?.error?.message || 'Could not load your attendance.';
        this.cdr.detectChanges();
      })
    });
  }

  private tickLive(): void {
    const t = this.data?.todayStatus;
    if (!t || !t.hasRecord || !t.checkInTime || t.checkOutTime) {
      this.liveLabel = t?.workingHoursLabel ?? '00h 00m';
      return;
    }
    if (t.onBreak) { this.liveLabel = t.workingHoursLabel; return; }
    // Recompute from check-in time minus completed-break minutes.
    const [h, m] = t.checkInTime.split(':').map(Number);
    const now = new Date();
    const ci = new Date(now); ci.setHours(h, m, 0, 0);
    const breakMin = (t.breaks || []).reduce((s, b) => s + (b.minutes || 0), 0);
    let mins = Math.floor((now.getTime() - ci.getTime()) / 60000) - breakMin;
    if (mins < 0) mins = 0;
    this.liveLabel = `${String(Math.floor(mins / 60)).padStart(2, '0')}h ${String(mins % 60).padStart(2, '0')}m`;
  }

  // ── GPS ───────────────────────────────────────────────────────────────────
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
          ? 'Location permission denied. You can still check in — the geo-fence just won\'t be verified.'
          : `Could not get your location (${err.message || 'unknown'}). You can still check in.`;
        this.cdr.detectChanges();
      }),
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 30_000 }
    );
  }

  // ── Primary actions ─────────────────────────────────────────────────────────
  get today() { return this.data?.todayStatus; }
  get canCheckIn(): boolean { return !!this.today && !this.today.hasRecord; }
  get canCheckOut(): boolean { return !!this.today && this.today.hasRecord && !this.today.checkOutTime && !this.today.onBreak; }
  get canBreakOut(): boolean { return !!this.today && this.today.hasRecord && !this.today.checkOutTime && !this.today.onBreak; }
  get canBreakIn(): boolean { return !!this.today && !!this.today.onBreak; }

  startCheckIn(): void {
    if (this.busy || !this.canCheckIn) return;
    if (this.data?.requireSelfie) { this.openSelfie('in'); return; }
    this.submitCheckIn(null);
  }

  startCheckOut(): void {
    if (this.busy || !this.canCheckOut) return;
    if (this.data?.requireSelfie) { this.openSelfie('out'); return; }
    this.submitCheckOut(null);
  }

  private submitCheckIn(selfie: string | null): void {
    this.busy = true; this.actionError = ''; this.cdr.detectChanges();
    this.svc.selfCheckIn({
      latitude: this.latitude, longitude: this.longitude, notes: null,
      selfieBase64: selfie
    }).subscribe({
      next: (res) => this.afterAction(res.success, res.message),
      error: (err) => this.afterAction(false, err?.error?.message)
    });
  }

  private submitCheckOut(selfie: string | null): void {
    this.busy = true; this.actionError = ''; this.cdr.detectChanges();
    this.svc.selfCheckOut({
      latitude: this.latitude, longitude: this.longitude, selfieBase64: selfie
    }).subscribe({
      next: (res) => this.afterAction(res.success, res.message),
      error: (err) => this.afterAction(false, err?.error?.message)
    });
  }

  doBreakOut(): void {
    if (this.busy || !this.canBreakOut) return;
    this.busy = true; this.actionError = ''; this.cdr.detectChanges();
    this.svc.breakOut().subscribe({
      next: (res) => this.afterAction(res.success, res.message),
      error: (err) => this.afterAction(false, err?.error?.message)
    });
  }

  doBreakIn(): void {
    if (this.busy || !this.canBreakIn) return;
    this.busy = true; this.actionError = ''; this.cdr.detectChanges();
    this.svc.breakIn().subscribe({
      next: (res) => this.afterAction(res.success, res.message),
      error: (err) => this.afterAction(false, err?.error?.message)
    });
  }

  private afterAction(success: boolean, message?: string): void {
    this.zone.run(() => {
      this.busy = false;
      this.closeSelfie();
      if (!success) this.actionError = message || 'Action failed.';
      else this.actionError = '';
      this.load(true);
      this.cdr.detectChanges();
    });
  }

  // ── Selfie webcam ───────────────────────────────────────────────────────────
  openSelfie(forWhich: SelfieFor): void {
    this.selfieFor = forWhich;
    this.selfieOpen = true;
    this.capturedSelfie = null;
    this.cameraError = '';
    this.cameraReady = false;
    this.cdr.detectChanges();
    this.startCamera();
  }

  closeSelfie(): void {
    this.selfieOpen = false;
    this.capturedSelfie = null;
    this.stopCamera();
    this.cdr.detectChanges();
  }

  private startCamera(): void {
    if (!navigator.mediaDevices?.getUserMedia) {
      this.cameraError = 'Camera not available on this device/browser.';
      this.cdr.detectChanges();
      return;
    }
    navigator.mediaDevices.getUserMedia({ video: { facingMode: 'user', width: 480, height: 480 }, audio: false })
      .then((stream) => this.zone.run(() => {
        this.mediaStream = stream;
        const v = this.videoRef?.nativeElement;
        if (v) { v.srcObject = stream; v.play().catch(() => {}); }
        this.cameraReady = true;
        this.cdr.detectChanges();
      }))
      .catch((err) => this.zone.run(() => {
        this.cameraError = err?.name === 'NotAllowedError'
          ? 'Camera permission denied. Please allow camera access to verify your attendance.'
          : `Could not start camera (${err?.message || 'unknown'}).`;
        this.cdr.detectChanges();
      }));
  }

  private stopCamera(): void {
    this.mediaStream?.getTracks().forEach(t => t.stop());
    this.mediaStream = null;
    this.cameraReady = false;
  }

  capture(): void {
    const v = this.videoRef?.nativeElement;
    const c = this.canvasRef?.nativeElement;
    if (!v || !c) return;
    const size = 480;
    c.width = size; c.height = size;
    const ctx = c.getContext('2d');
    if (!ctx) return;
    // Mirror horizontally so the captured image matches the on-screen preview.
    ctx.translate(size, 0); ctx.scale(-1, 1);
    const vw = v.videoWidth || size, vh = v.videoHeight || size;
    const side = Math.min(vw, vh);
    ctx.drawImage(v, (vw - side) / 2, (vh - side) / 2, side, side, 0, 0, size, size);
    this.capturedSelfie = c.toDataURL('image/jpeg', 0.8);
    this.stopCamera();
    this.cdr.detectChanges();
  }

  retake(): void {
    this.capturedSelfie = null;
    this.startCamera();
    this.cdr.detectChanges();
  }

  confirmSelfie(): void {
    if (!this.capturedSelfie) return;
    const base64 = this.capturedSelfie.split(',')[1] ?? null;
    if (this.selfieFor === 'in') this.submitCheckIn(base64);
    else this.submitCheckOut(base64);
  }

  // ── Correction requests ─────────────────────────────────────────────────────
  loadRequests(): void {
    this.svc.getMyRequests().subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.myRequests = res.data;
        this.cdr.detectChanges();
      }),
      error: () => {}
    });
  }

  openRequest(): void {
    this.reqError = '';
    this.reqForm = {
      requestDate: this.data?.today ?? new Date().toISOString().slice(0, 10),
      requestType: 'TimeCorrection', requestedCheckInTime: '', requestedCheckOutTime: '', reason: ''
    };
    this.reqOpen = true;
    this.cdr.detectChanges();
  }

  submitRequest(): void {
    if (this.reqBusy) return;
    if (!this.reqForm.reason.trim()) { this.reqError = 'Please describe the reason.'; this.cdr.detectChanges(); return; }
    this.reqBusy = true; this.reqError = '';
    this.svc.submitRequest({
      requestDate: this.reqForm.requestDate,
      requestType: this.reqForm.requestType,
      requestedCheckInTime: this.reqForm.requestedCheckInTime || null,
      requestedCheckOutTime: this.reqForm.requestedCheckOutTime || null,
      requestedStatus: null,
      reason: this.reqForm.reason.trim()
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.reqBusy = false;
        if (res.success) { this.reqOpen = false; this.loadRequests(); }
        else this.reqError = res.message || 'Could not submit.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.reqBusy = false;
        this.reqError = err?.error?.message || 'Could not submit.';
        this.cdr.detectChanges();
      })
    });
  }

  cancelRequest(id: number): void {
    this.svc.cancelRequest(id).subscribe({
      next: () => this.zone.run(() => { this.loadRequests(); this.cdr.detectChanges(); }),
      error: () => {}
    });
  }

  // ── View helpers ────────────────────────────────────────────────────────────
  formatDistance(d: number | null): string {
    if (d == null) return '—';
    return d < 1000 ? `${Math.round(d)} m` : `${(d / 1000).toFixed(2)} km`;
  }

  greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  }

  // Calendar grid for the current month (leading blanks for weekday alignment).
  get calendarCells(): { date: string | null; day: number; status: string | null }[] {
    if (!this.data) return [];
    const map = new Map(this.data.calendar.map(c => [c.date, c.status]));
    const [y, mo] = [this.data.month.year, this.data.month.month];
    const first = new Date(y, mo - 1, 1);
    const daysInMonth = new Date(y, mo, 0).getDate();
    const lead = first.getDay(); // 0=Sun
    const cells: { date: string | null; day: number; status: string | null }[] = [];
    for (let i = 0; i < lead; i++) cells.push({ date: null, day: 0, status: null });
    for (let d = 1; d <= daysInMonth; d++) {
      const ds = `${y}-${String(mo).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
      cells.push({ date: ds, day: d, status: map.get(ds) ?? null });
    }
    return cells;
  }

  dayClass(status: string | null): string {
    if (!status) return '';
    switch (status) {
      case 'Present': case 'OnTime': case 'Overtime': return 'd-present';
      case 'Late': return 'd-late';
      case 'EarlyLeave': return 'd-late';
      case 'HalfDay': return 'd-half';
      case 'Absent': return 'd-absent';
      case 'Leave': return 'd-leave';
      case 'Holiday': return 'd-holiday';
      case 'OffdayWork': case 'HolidayWork': return 'd-extra';
      default: return '';
    }
  }

  statusLabel(status: string): string {
    return status.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  // OpenStreetMap embed (no API key, no JS library) — pins the check-in location.
  get mapUrl(): SafeResourceUrl | null {
    const t = this.today;
    if (!t || t.latitude == null || t.longitude == null) return null;
    const key = `${t.latitude},${t.longitude}`;
    if (key === this.lastMapKey && this.mapUrlCache) return this.mapUrlCache;
    const lat = t.latitude, lon = t.longitude;
    const d = 0.0025; // ~250 m bbox
    const bbox = `${(lon - d).toFixed(5)},${(lat - d).toFixed(5)},${(lon + d).toFixed(5)},${(lat + d).toFixed(5)}`;
    const url = `https://www.openstreetmap.org/export/embed.html?bbox=${bbox}&layer=mapnik&marker=${lat.toFixed(6)},${lon.toFixed(6)}`;
    this.lastMapKey = key;
    this.mapUrlCache = this.sanitizer.bypassSecurityTrustResourceUrl(url);
    return this.mapUrlCache;
  }

  get osmLink(): string {
    const t = this.today;
    if (!t || t.latitude == null || t.longitude == null) return '#';
    return `https://www.openstreetmap.org/?mlat=${t.latitude}&mlon=${t.longitude}#map=18/${t.latitude}/${t.longitude}`;
  }
}
