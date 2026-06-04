import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { FixedAssetService } from '../../../services/fixed-asset.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  FixedAssetDto, FIXED_ASSET_CATEGORIES, FIXED_ASSET_STATUSES
} from '../../../models/fixed-asset.models';

@Component({
  selector: 'app-fixed-asset-list',
  standalone: false,
  templateUrl: './fixed-asset-list.component.html',
  styleUrl: './fixed-asset-list.component.scss'
})
export class FixedAssetListComponent implements OnInit {
  assets: FixedAssetDto[] = [];
  loading = false;
  totalCount = 0;
  actionError = '';
  actionMessage = '';

  readonly categories = FIXED_ASSET_CATEGORIES;
  readonly statuses = FIXED_ASSET_STATUSES;

  filterStatus: string | null = null;
  filterCategory: string | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editing: FixedAssetDto | null = null;
  form!: FormGroup;

  // Dispose
  disposeVisible = false;
  disposeTarget: FixedAssetDto | null = null;
  disposeForm!: FormGroup;
  disposing = false;
  disposeError = '';

  // Run depreciation
  runVisible = false;
  runYear: number;
  runMonth: number;
  running = false;
  runError = '';

  constructor(
    private svc: FixedAssetService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {
    const now = new Date();
    this.runYear = now.getFullYear();
    this.runMonth = now.getMonth() + 1;
  }

  ngOnInit(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      category: ['Machinery', Validators.required],
      location: ['', Validators.maxLength(150)],
      acquisitionDate: [this.todayIso(), Validators.required],
      acquisitionCost: [0, [Validators.required, Validators.min(0.01)]],
      salvageValue: [0, [Validators.required, Validators.min(0)]],
      usefulLifeYears: [5, [Validators.required, Validators.min(1), Validators.max(60)]],
      notes: ['', Validators.maxLength(2000)]
    });
    this.disposeForm = this.fb.group({
      disposalDate: [this.todayIso(), Validators.required],
      disposalProceeds: [0, [Validators.required, Validators.min(0)]],
      notes: ['', Validators.maxLength(1000)],
      isWriteOff: [false]
    });
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().substring(0, 10); }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }

  categoryLabel(c: string): string { return this.categories.find(x => x.value === c)?.label ?? c; }

  statusBadgeClass(s: string): string {
    switch (s) {
      case 'Active': return 'active';
      case 'Disposed': return 'disposed';
      case 'WrittenOff': return 'writtenoff';
      default: return '';
    }
  }

  monthlyPreview(): number {
    const cost = Number(this.form?.get('acquisitionCost')?.value || 0);
    const salv = Number(this.form?.get('salvageValue')?.value || 0);
    const yrs = Number(this.form?.get('usefulLifeYears')?.value || 0);
    if (yrs <= 0) return 0;
    return Math.round(((cost - salv) / (yrs * 12)) * 100) / 100;
  }

  ymLabel(ym: number | null): string {
    if (!ym) return '—';
    const y = Math.floor(ym / 100);
    const m = ym % 100;
    return `${y}-${m.toString().padStart(2, '0')}`;
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters, this.filterStatus ?? undefined, this.filterCategory ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.assets = res.data.items;
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

  // ── Create / Edit / View ────────────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editing = null;
    this.dialogError = '';
    this.form.reset({
      name: '', category: 'Machinery', location: '',
      acquisitionDate: this.todayIso(),
      acquisitionCost: 0, salvageValue: 0, usefulLifeYears: 5, notes: ''
    });
    this.form.enable();
    this.dialogVisible = true;
  }

  openEdit(a: FixedAssetDto): void {
    this.dialogMode = a.status === 'Active' ? 'edit' : 'view';
    this.editing = a;
    this.dialogError = '';
    this.form.reset({
      name: a.name,
      category: a.category,
      location: a.location ?? '',
      acquisitionDate: a.acquisitionDate,
      acquisitionCost: a.acquisitionCost,
      salvageValue: a.salvageValue,
      usefulLifeYears: a.usefulLifeYears,
      notes: a.notes ?? ''
    });
    if (this.dialogMode === 'view') this.form.disable(); else this.form.enable();
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const body = {
      name: v.name.trim(),
      category: v.category,
      location: (v.location as string)?.trim() || null,
      machineId: null,
      acquisitionDate: v.acquisitionDate,
      acquisitionCost: Number(v.acquisitionCost),
      salvageValue: Number(v.salvageValue),
      usefulLifeYears: Number(v.usefulLifeYears),
      notes: (v.notes as string)?.trim() || null
    };
    const obs: any = this.dialogMode === 'create'
      ? this.svc.create(body)
      : this.svc.update(this.editing!.id, body);
    obs.subscribe({
      next: (res: any) => this.zone.run(() => {
        this.dialogSaving = false;
        if (res.success) { this.dialogVisible = false; this.actionMessage = res.message || 'Saved.'; this.load(); }
        else this.dialogError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err: any) => this.zone.run(() => {
        this.dialogSaving = false;
        this.dialogError = err?.error?.message || 'Save failed.';
        this.cdr.detectChanges();
      })
    });
  }

  doDelete(a: FixedAssetDto): void {
    if (!confirm(`Delete ${a.code} ${a.name}? (only allowed if no depreciation has run)`)) return;
    this.svc.delete(a.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success) { this.actionMessage = res.message || 'Deleted.'; this.load(); }
        else this.actionError = res.message || 'Delete failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Dispose ─────────────────────────────────────────────────────────────

  openDispose(a: FixedAssetDto): void {
    this.disposeTarget = a;
    this.disposeError = '';
    this.disposeForm.reset({
      disposalDate: this.todayIso(),
      disposalProceeds: 0,
      notes: '',
      isWriteOff: false
    });
    this.disposeVisible = true;
  }

  doDispose(): void {
    if (!this.disposeTarget || this.disposeForm.invalid || this.disposing) return;
    this.disposing = true; this.disposeError = ''; this.cdr.detectChanges();
    const v = this.disposeForm.getRawValue();
    this.svc.dispose(this.disposeTarget.id, {
      disposalDate: v.disposalDate,
      disposalProceeds: Number(v.disposalProceeds),
      notes: (v.notes as string)?.trim() || null,
      isWriteOff: !!v.isWriteOff
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.disposing = false;
        if (res.success) { this.disposeVisible = false; this.actionMessage = res.message || 'Disposed.'; this.load(); }
        else this.disposeError = res.message || 'Dispose failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.disposing = false;
        this.disposeError = err?.error?.message || 'Dispose failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Run Depreciation ────────────────────────────────────────────────────

  openRun(): void {
    this.runError = '';
    this.runVisible = true;
  }

  doRun(): void {
    if (this.running) return;
    this.running = true; this.runError = ''; this.cdr.detectChanges();
    this.svc.runDepreciation({ year: this.runYear, month: this.runMonth }).subscribe({
      next: (res) => this.zone.run(() => {
        this.running = false;
        if (res.success) { this.runVisible = false; this.actionMessage = res.message || 'Depreciation posted.'; this.load(); }
        else this.runError = res.message || 'Run failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.running = false;
        this.runError = err?.error?.message || 'Run failed.';
        this.cdr.detectChanges();
      })
    });
  }
}
