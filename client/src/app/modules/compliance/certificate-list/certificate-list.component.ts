import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ComplianceService } from '../../../services/compliance.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  ComplianceCertificateDto, CertificateType, ExpiryStatus,
  CERTIFICATE_TYPES, EXPIRY_STATUSES
} from '../../../models/compliance.models';

@Component({
  selector: 'app-certificate-list',
  standalone: false,
  templateUrl: './certificate-list.component.html',
  styleUrl: './certificate-list.component.scss'
})
export class CertificateListComponent implements OnInit {
  certs: ComplianceCertificateDto[] = [];
  loading = false;
  totalCount = 0;
  filterType: CertificateType | null = null;
  filterStatus: ExpiryStatus | null = null;
  includeInactive = false;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly types = CERTIFICATE_TYPES;
  readonly statuses = EXPIRY_STATUSES;

  canManage = false;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  attachmentsVisible = false;
  attachmentsTarget: ComplianceCertificateDto | null = null;

  deleteDialogVisible = false;
  deleting: ComplianceCertificateDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(private svc: ComplianceService, private auth: AuthService,
              private fb: FormBuilder, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('Compliance.ManageCertificates');
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      certificateType: ['BSCI' as CertificateType, Validators.required],
      issuingAuthority: ['', Validators.maxLength(200)],
      certificateNumber: ['', Validators.maxLength(100)],
      issuedDate: [this.todayIso(), Validators.required],
      expiryDate: [this.oneYearAhead(), Validators.required],
      notes: ['', Validators.maxLength(2000)],
      isActive: [true]
    });
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }
  private oneYearAhead(): string {
    const d = new Date(); d.setFullYear(d.getFullYear() + 1);
    return d.toISOString().slice(0, 10);
  }

  load(): void {
    this.loading = true;
    this.svc.getCertificates(this.parameters, this.filterType, this.filterStatus, this.includeInactive).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.certs = res.data.items; this.totalCount = res.data.totalCount; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearch(v: string): void { if (this.searchTimer) clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => { this.parameters.search = v; this.parameters.page = 1; this.load(); }, 350); }
  onPage(e: any): void { this.parameters.page = Math.floor(e.first / e.rows) + 1; this.parameters.pageSize = e.rows; this.load(); }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = '';
    this.form.reset({
      name: '', certificateType: 'BSCI', issuingAuthority: '', certificateNumber: '',
      issuedDate: this.todayIso(), expiryDate: this.oneYearAhead(), notes: '', isActive: true
    });
    this.dialogVisible = true;
  }
  openEdit(c: ComplianceCertificateDto): void {
    this.dialogMode = 'edit'; this.editingId = c.id; this.dialogError = '';
    this.form.reset({
      name: c.name, certificateType: c.certificateType,
      issuingAuthority: c.issuingAuthority ?? '', certificateNumber: c.certificateNumber ?? '',
      issuedDate: c.issuedDate, expiryDate: c.expiryDate,
      notes: c.notes ?? '', isActive: c.isActive
    });
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = {
      name: v.name.trim(), certificateType: v.certificateType,
      issuingAuthority: (v.issuingAuthority as string)?.trim() || null,
      certificateNumber: (v.certificateNumber as string)?.trim() || null,
      issuedDate: v.issuedDate, expiryDate: v.expiryDate,
      notes: (v.notes as string)?.trim() || null
    };
    const done = (res: any) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.createCertificate(base).subscribe({ next: done, error: err });
    else this.svc.updateCertificate(this.editingId!, { ...base, isActive: v.isActive }).subscribe({ next: done, error: err });
  }

  openAttachments(c: ComplianceCertificateDto): void { this.attachmentsTarget = c; this.attachmentsVisible = true; }

  confirmDelete(c: ComplianceCertificateDto): void { this.deleting = c; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.deleteCertificate(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }

  expirySeverity(s: string): 'success' | 'warn' | 'danger' {
    return s === 'Active' ? 'success' : s === 'ExpiringSoon' ? 'warn' : 'danger';
  }

  daysBadge(c: ComplianceCertificateDto): string {
    if (c.daysUntilExpiry < 0) return `Expired ${-c.daysUntilExpiry}d ago`;
    if (c.daysUntilExpiry === 0) return 'Expires today';
    return `${c.daysUntilExpiry}d left`;
  }
}
