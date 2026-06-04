import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { EmailService } from '../../../services/email.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  SentEmailDto, EMAIL_STATUSES, EMAIL_SOURCE_TYPES
} from '../../../models/email.models';

@Component({
  selector: 'app-sent-email-list',
  standalone: false,
  templateUrl: './sent-email-list.component.html',
  styleUrl: './sent-email-list.component.scss'
})
export class SentEmailListComponent implements OnInit {
  emails: SentEmailDto[] = [];
  loading = false;
  totalCount = 0;
  readonly statuses = EMAIL_STATUSES;
  readonly sourceTypes = EMAIL_SOURCE_TYPES;

  filterStatus: string | null = null;
  filterSourceType: string | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  viewVisible = false;
  viewing: SentEmailDto | null = null;
  viewBody = '';

  constructor(
    private svc: EmailService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters,
      this.filterStatus ?? undefined, this.filterSourceType ?? undefined
    ).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.emails = res.data.items;
          this.totalCount = res.data.totalCount;
        }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearchChange(value: string): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.parameters.search = value;
      this.parameters.page = 1;
      this.load();
    }, 400);
  }

  onPageChange(event: any): void {
    this.parameters.page = Math.floor(event.first / event.rows) + 1;
    this.parameters.pageSize = event.rows;
    this.load();
  }

  sourceTypeLabel(t: string | null): string {
    if (!t) return '—';
    return this.sourceTypes.find(x => x.value === t)?.label ?? t;
  }

  openView(e: SentEmailDto): void {
    this.viewing = e;
    // Need full body — list endpoint doesn't return body. For v1 simplicity, fetch via the existing
    // preview endpoint isn't right (that re-renders). We just keep the body NOT shown for sent log
    // since we'd need an /api/emails/{id} endpoint. For now, display subject + metadata + error.
    this.viewBody = '';
    this.viewVisible = true;
  }
}
