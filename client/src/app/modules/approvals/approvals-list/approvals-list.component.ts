import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ApprovalService } from '../../../services/approval.service';
import { ApprovalRequestDto, APPROVAL_STATUSES } from '../../../models/approval.models';

@Component({
  selector: 'app-approvals-list',
  standalone: false,
  templateUrl: './approvals-list.component.html',
  styleUrl: './approvals-list.component.scss'
})
export class ApprovalsListComponent implements OnInit {
  readonly statuses = APPROVAL_STATUSES;

  mode: 'inbox' | 'all' = 'inbox';
  filterStatus: string | null = null;

  requests: ApprovalRequestDto[] = [];
  loading = false;
  error = '';

  // Decision dialog
  decisionVisible = false;
  decisionMode: 'approve' | 'reject' = 'approve';
  decisionTarget: ApprovalRequestDto | null = null;
  decisionComment = '';
  decisionSaving = false;
  decisionError = '';

  // Detail dialog
  detailVisible = false;
  detail: ApprovalRequestDto | null = null;

  constructor(
    private service: ApprovalService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void { this.load(); }

  setMode(mode: 'inbox' | 'all'): void {
    if (this.mode === mode) return;
    this.mode = mode;
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();

    const obs = this.mode === 'inbox'
      ? this.service.inbox()
      : this.service.getAll(this.filterStatus ?? undefined);

    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        this.requests = res.success && res.data ? res.data : [];
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => {
        this.loading = false;
        this.requests = [];
        this.cdr.detectChanges();
      })
    });
  }

  openDecision(req: ApprovalRequestDto, mode: 'approve' | 'reject'): void {
    this.decisionTarget = req;
    this.decisionMode = mode;
    this.decisionComment = '';
    this.decisionError = '';
    this.decisionVisible = true;
  }

  confirmDecision(): void {
    if (!this.decisionTarget || this.decisionSaving) return;
    this.decisionSaving = true;
    this.decisionError = '';
    this.cdr.detectChanges();

    const id = this.decisionTarget.id;
    const comment = this.decisionComment?.trim() || null;
    const obs = this.decisionMode === 'approve'
      ? this.service.approve(id, comment)
      : this.service.reject(id, comment);

    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.decisionSaving = false;
        if (res.success) {
          this.decisionVisible = false;
          this.decisionTarget = null;
          this.load();
        } else {
          this.decisionError = res.message || 'Action failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.decisionSaving = false;
        this.decisionError = err?.error?.message || 'Action failed.';
        this.cdr.detectChanges();
      })
    });
  }

  openDetail(req: ApprovalRequestDto): void {
    this.detail = req;
    this.detailVisible = true;
  }

  formatDate(d: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' });
  }
}
