import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AuditLogService } from '../../../services/audit-log.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { AUDIT_ACTIONS, AuditLogEntryDto } from '../../../models/audit-log.models';

interface DiffRow {
  field: string;
  oldValue: string;
  newValue: string;
  changed: boolean;
}

@Component({
  selector: 'app-audit-log-list',
  standalone: false,
  templateUrl: './audit-log-list.component.html',
  styleUrl: './audit-log-list.component.scss'
})
export class AuditLogListComponent implements OnInit {

  entries: AuditLogEntryDto[] = [];
  loading = false;
  totalCount = 0;

  filterEntityType: string | null = null;
  filterAction: string | null = null;
  filterUserName = '';
  fromDate: string | null = null;
  toDate: string | null = null;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;
  userTimer: any = null;

  readonly actions = AUDIT_ACTIONS;
  entityTypes: { label: string; value: string }[] = [];

  // Detail dialog
  viewDialogVisible = false;
  viewing: AuditLogEntryDto | null = null;
  diffRows: DiffRow[] = [];

  constructor(
    private auditService: AuditLogService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadEntityTypes();
    this.load();
  }

  private loadEntityTypes(): void {
    this.auditService.getEntityTypes().subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.entityTypes = res.data.map(t => ({ label: this.shortType(t), value: t }));
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.auditService.getAll(
      this.parameters,
      this.filterEntityType ?? undefined,
      this.filterAction ?? undefined,
      this.filterUserName?.trim() || undefined,
      this.fromDate ?? undefined,
      this.toDate ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.entries = res.data.items;
            this.totalCount = res.data.totalCount;
          }
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); });
      }
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

  onUserChange(): void {
    if (this.userTimer) clearTimeout(this.userTimer);
    this.userTimer = setTimeout(() => {
      this.parameters.page = 1;
      this.load();
    }, 400);
  }

  onPageChange(event: any): void {
    this.parameters.page = Math.floor(event.first / event.rows) + 1;
    this.parameters.pageSize = event.rows;
    this.load();
  }

  openView(e: AuditLogEntryDto): void {
    this.viewing = e;
    this.diffRows = this.buildDiff(e);
    this.viewDialogVisible = true;
  }

  // Build a field-level old→new diff from the two JSON snapshots
  private buildDiff(e: AuditLogEntryDto): DiffRow[] {
    const oldObj = this.parseJson(e.oldValuesJson);
    const newObj = this.parseJson(e.newValuesJson);
    const keys = Array.from(new Set([...Object.keys(oldObj), ...Object.keys(newObj)])).sort();
    return keys.map(k => {
      const ov = this.fmt(oldObj[k]);
      const nv = this.fmt(newObj[k]);
      return { field: k, oldValue: ov, newValue: nv, changed: ov !== nv };
    });
  }

  private parseJson(json: string | null): Record<string, any> {
    if (!json) return {};
    try { return JSON.parse(json) ?? {}; } catch { return {}; }
  }

  private fmt(v: any): string {
    if (v === null || v === undefined) return '—';
    if (typeof v === 'object') return JSON.stringify(v);
    return String(v);
  }

  shortType(entityType: string): string {
    // Strip namespace — show just the class name
    const parts = entityType.split('.');
    return parts[parts.length - 1];
  }

  actionClass(action: string): string {
    switch (action) {
      case 'Insert': return 'insert';
      case 'Update': return 'update';
      case 'Delete': return 'delete';
      default:       return '';
    }
  }
}
