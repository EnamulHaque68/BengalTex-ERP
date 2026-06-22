import { ChangeDetectorRef, Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeUrl, SafeResourceUrl } from '@angular/platform-browser';
import { AttendanceService } from '../../../services/attendance.service';
import {
  TeamAttendanceDto, TeamAttendanceRowDto, AttendanceRequestDto
} from '../../../models/attendance.models';

@Component({
  selector: 'app-team-attendance',
  standalone: false,
  templateUrl: './team-attendance.component.html',
  styleUrl: './team-attendance.component.scss'
})
export class TeamAttendanceComponent implements OnInit, OnDestroy {
  tab: 'team' | 'requests' = 'team';

  loading = true;
  error = '';
  data: TeamAttendanceDto | null = null;

  // filters
  fromDate = '';
  toDate = '';
  onlyFlagged = false;

  // requests
  reqLoading = false;
  reqError = '';
  reqStatus = 'Pending';
  requests: AttendanceRequestDto[] = [];

  // selfie review
  selfieOpen = false;
  selfieLoading = false;
  selfieUrl: SafeUrl | null = null;
  selfieRow: TeamAttendanceRowDto | null = null;
  private objectUrl: string | null = null;

  // map review (location verification)
  mapOpen = false;
  mapRow: TeamAttendanceRowDto | null = null;
  mapWhich: 'in' | 'out' = 'in';
  mapUrl: SafeResourceUrl | null = null;

  // decision dialog (shared for approve/reject of check-in OR request)
  decideOpen = false;
  decideMode: 'attendance' | 'request' = 'attendance';
  decideApprove = true;
  decideId = 0;
  decideNote = '';
  decideBusy = false;
  decideError = '';
  decideTitle = '';

  constructor(
    private svc: AttendanceService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    const today = new Date().toISOString().slice(0, 10);
    this.fromDate = today;
    this.toDate = today;
    this.loadTeam();
  }

  ngOnDestroy(): void { this.revokeSelfie(); }

  // ── Team ──
  loadTeam(): void {
    this.loading = true; this.error = '';
    this.svc.getTeamAttendance(this.fromDate || undefined, this.toDate || undefined, this.onlyFlagged).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.data = res.data;
        else this.error = res.message || 'Could not load team attendance.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loading = false;
        this.error = err?.error?.message || 'Could not load team attendance.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Requests ──
  switchTab(t: 'team' | 'requests'): void {
    this.tab = t;
    if (t === 'requests' && this.requests.length === 0) this.loadRequests();
  }

  loadRequests(): void {
    this.reqLoading = true; this.reqError = '';
    this.svc.getTeamRequests(this.reqStatus).subscribe({
      next: (res) => this.zone.run(() => {
        this.reqLoading = false;
        if (res.success && res.data) this.requests = res.data;
        else this.reqError = res.message || 'Could not load requests.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.reqLoading = false;
        this.reqError = err?.error?.message || 'Could not load requests.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Selfie review ──
  viewSelfie(row: TeamAttendanceRowDto, which: 'in' | 'out'): void {
    this.selfieRow = row;
    this.selfieOpen = true;
    this.selfieLoading = true;
    this.revokeSelfie();
    this.cdr.detectChanges();
    this.svc.getSelfieBlob(row.id, which).subscribe({
      next: (blob) => this.zone.run(() => {
        this.objectUrl = URL.createObjectURL(blob);
        this.selfieUrl = this.sanitizer.bypassSecurityTrustUrl(this.objectUrl);
        this.selfieLoading = false;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => {
        this.selfieLoading = false;
        this.cdr.detectChanges();
      })
    });
  }

  closeSelfie(): void { this.selfieOpen = false; this.revokeSelfie(); this.cdr.detectChanges(); }

  private revokeSelfie(): void {
    if (this.objectUrl) { URL.revokeObjectURL(this.objectUrl); this.objectUrl = null; }
    this.selfieUrl = null;
  }

  // ── Map review (confirm where the employee checked in/out) ──
  hasGeo(row: TeamAttendanceRowDto): boolean {
    return row.latitude != null && row.longitude != null;
  }

  openMap(row: TeamAttendanceRowDto, which: 'in' | 'out' = 'in'): void {
    this.mapRow = row;
    this.mapWhich = which;
    this.mapOpen = true;
    const lat = row.latitude, lon = row.longitude;
    if (lat != null && lon != null) {
      const d = 0.0025; // ~250 m bbox
      const bbox = `${(lon - d).toFixed(5)},${(lat - d).toFixed(5)},${(lon + d).toFixed(5)},${(lat + d).toFixed(5)}`;
      const url = `https://www.openstreetmap.org/export/embed.html?bbox=${bbox}&layer=mapnik&marker=${lat.toFixed(6)},${lon.toFixed(6)}`;
      this.mapUrl = this.sanitizer.bypassSecurityTrustResourceUrl(url);
    } else {
      this.mapUrl = null;
    }
    this.cdr.detectChanges();
  }

  closeMap(): void { this.mapOpen = false; this.mapUrl = null; this.cdr.detectChanges(); }

  osmLink(row: TeamAttendanceRowDto): string {
    if (row.latitude == null || row.longitude == null) return '#';
    return `https://www.openstreetmap.org/?mlat=${row.latitude}&mlon=${row.longitude}#map=18/${row.latitude}/${row.longitude}`;
  }

  // ── Decision dialog ──
  openDecide(mode: 'attendance' | 'request', id: number, approve: boolean, label: string): void {
    this.decideMode = mode;
    this.decideId = id;
    this.decideApprove = approve;
    this.decideNote = '';
    this.decideError = '';
    this.decideOpen = true;
    this.decideTitle = `${approve ? 'Approve' : 'Reject'} ${label}`;
    this.cdr.detectChanges();
  }

  // Approve check-in directly (no note needed); reject opens the dialog for a reason.
  quickApproveAttendance(row: TeamAttendanceRowDto): void {
    this.submitAttendanceDecision(row.id, true, undefined);
  }

  confirmDecide(): void {
    if (!this.decideApprove && !this.decideNote.trim()) {
      this.decideError = this.decideMode === 'attendance' ? 'A rejection reason is required.' : 'A note is required to reject.';
      this.cdr.detectChanges();
      return;
    }
    this.decideBusy = true; this.decideError = '';
    if (this.decideMode === 'attendance') this.submitAttendanceDecision(this.decideId, this.decideApprove, this.decideNote.trim());
    else this.submitRequestDecision(this.decideId, this.decideApprove, this.decideNote.trim());
  }

  private submitAttendanceDecision(id: number, approve: boolean, reason?: string): void {
    this.svc.approveAttendance(id, approve, reason).subscribe({
      next: (res) => this.afterDecide(res.success, res.message, 'team'),
      error: (err) => this.afterDecide(false, err?.error?.message, 'team')
    });
  }

  private submitRequestDecision(id: number, approve: boolean, note?: string): void {
    this.svc.decideRequest(id, approve, note).subscribe({
      next: (res) => this.afterDecide(res.success, res.message, 'requests'),
      error: (err) => this.afterDecide(false, err?.error?.message, 'requests')
    });
  }

  private afterDecide(success: boolean, message: string | undefined, reload: 'team' | 'requests'): void {
    this.zone.run(() => {
      this.decideBusy = false;
      if (!success) { this.decideError = message || 'Action failed.'; this.cdr.detectChanges(); return; }
      this.decideOpen = false;
      if (reload === 'team') this.loadTeam();
      else this.loadRequests();
      this.cdr.detectChanges();
    });
  }

  // ── View helpers ──
  statusLabel(s: string): string { return s.replace(/([a-z])([A-Z])/g, '$1 $2'); }

  formatDistance(d: number | null): string {
    if (d == null) return '';
    return d < 1000 ? `${Math.round(d)} m` : `${(d / 1000).toFixed(2)} km`;
  }

  rowClass(row: TeamAttendanceRowDto): string {
    if (row.flags.some(f => f.severity === 'critical')) return 'row-critical';
    if (row.flags.some(f => f.severity === 'warning')) return 'row-warning';
    return '';
  }
}
