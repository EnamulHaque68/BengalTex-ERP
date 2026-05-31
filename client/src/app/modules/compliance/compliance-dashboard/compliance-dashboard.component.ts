import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ComplianceService } from '../../../services/compliance.service';
import { ComplianceDashboardDto, ComplianceCertificateDto, AuditFindingDto } from '../../../models/compliance.models';

@Component({
  selector: 'app-compliance-dashboard',
  standalone: false,
  templateUrl: './compliance-dashboard.component.html',
  styleUrl: './compliance-dashboard.component.scss'
})
export class ComplianceDashboardComponent implements OnInit {
  loading = false;
  data: ComplianceDashboardDto | null = null;

  constructor(private svc: ComplianceService, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.svc.getDashboard().subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.data = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  expirySeverity(s: string): 'success' | 'warn' | 'danger' | 'secondary' {
    return s === 'Active' ? 'success' : s === 'ExpiringSoon' ? 'warn' : 'danger';
  }

  severityColor(sev: string): string {
    switch (sev) {
      case 'Critical': return '#dc2626';
      case 'Major': return '#d97706';
      case 'Minor': return '#2563eb';
      case 'Observation': return '#6b7280';
      default: return '#6b7280';
    }
  }
}
