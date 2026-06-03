import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { GatePassService } from '../../../services/gate-pass.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  GatePassDto, GATE_PASS_TYPES, GATE_PASS_DIRECTIONS, GATE_PASS_STATUSES
} from '../../../models/gate-pass.models';

@Component({
  selector: 'app-gate-pass-list',
  standalone: false,
  templateUrl: './gate-pass-list.component.html',
  styleUrl: './gate-pass-list.component.scss'
})
export class GatePassListComponent implements OnInit {
  passes: GatePassDto[] = [];
  loading = false;
  totalCount = 0;
  actionError = '';
  actionMessage = '';
  rowActionId: number | null = null;

  readonly types = GATE_PASS_TYPES;
  readonly directions = GATE_PASS_DIRECTIONS;
  readonly statuses = GATE_PASS_STATUSES;

  filterStatus: string | null = null;
  filterType: string | null = null;
  filterFromDate: string | null = null;
  filterToDate: string | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Dialog
  dialogVisible = false;
  dialogSaving = false;
  dialogError = '';
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  editing: GatePassDto | null = null;
  form!: FormGroup;

  // Return dialog
  returnVisible = false;
  returnTarget: GatePassDto | null = null;
  returnNotes = '';
  returning = false;
  returnError = '';

  constructor(
    private service: GatePassService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      passDate: [this.todayIso(), Validators.required],
      passTime: [this.nowHm()],
      type: ['NonReturnableOut', Validators.required],
      direction: ['Out', Validators.required],
      vehicleNumber: ['', Validators.maxLength(30)],
      driverName: ['', Validators.maxLength(100)],
      driverPhone: ['', Validators.maxLength(30)],
      driverNidNumber: ['', Validators.maxLength(30)],
      transporterName: ['', Validators.maxLength(150)],
      visitorName: ['', Validators.maxLength(100)],
      visitorPhone: ['', Validators.maxLength(30)],
      visitorOrganization: ['', Validators.maxLength(150)],
      visitorPurpose: ['', Validators.maxLength(500)],
      itemDescription: ['', Validators.maxLength(1000)],
      quantity: ['', Validators.maxLength(100)],
      fromLocation: ['', Validators.maxLength(150)],
      toLocation: ['', Validators.maxLength(150)],
      sourceType: ['', Validators.maxLength(50)],
      sourceCode: ['', Validators.maxLength(100)],
      approvedByUser: ['', Validators.maxLength(100)],
      expectedReturnDate: [null],
      notes: ['', Validators.maxLength(2000)]
    });
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().substring(0, 10); }
  private nowHm(): string {
    const d = new Date();
    return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
  }

  typeLabel(t: string): string { return this.types.find(x => x.value === t)?.label ?? t; }

  statusBadgeClass(s: string): string {
    switch (s) {
      case 'Open': return 'open';
      case 'Returned': return 'returned';
      case 'Closed': return 'closed';
      case 'Cancelled': return 'cancelled';
      default: return '';
    }
  }

  typeBadgeClass(t: string): string {
    switch (t) {
      case 'NonReturnableOut': return 'nro';
      case 'ReturnableOut': return 'ro';
      case 'InwardReceipt': return 'in';
      case 'Visitor': return 'visitor';
      case 'Vehicle': return 'vehicle';
      default: return '';
    }
  }

  get isReturnable(): boolean {
    return this.form?.get('type')?.value === 'ReturnableOut';
  }
  get isVisitor(): boolean {
    return this.form?.get('type')?.value === 'Visitor';
  }
  get hasItemBlock(): boolean {
    const t = this.form?.get('type')?.value;
    return t === 'NonReturnableOut' || t === 'ReturnableOut' || t === 'InwardReceipt';
  }
  get hasVehicleBlock(): boolean {
    const t = this.form?.get('type')?.value;
    return t === 'NonReturnableOut' || t === 'ReturnableOut' || t === 'InwardReceipt' || t === 'Vehicle';
  }

  load(): void {
    this.loading = true;
    this.service.getAll(this.parameters,
      this.filterStatus ?? undefined, this.filterType ?? undefined,
      this.filterFromDate ?? undefined, this.filterToDate ?? undefined
    ).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.passes = res.data.items;
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
      passDate: this.todayIso(), passTime: this.nowHm(),
      type: 'NonReturnableOut', direction: 'Out',
      vehicleNumber: '', driverName: '', driverPhone: '', driverNidNumber: '', transporterName: '',
      visitorName: '', visitorPhone: '', visitorOrganization: '', visitorPurpose: '',
      itemDescription: '', quantity: '', fromLocation: '', toLocation: '',
      sourceType: '', sourceCode: '', approvedByUser: '', expectedReturnDate: null, notes: ''
    });
    this.form.enable();
    this.dialogVisible = true;
  }

  openEdit(g: GatePassDto): void {
    this.dialogMode = g.status === 'Open' ? 'edit' : 'view';
    this.editing = g;
    this.dialogError = '';
    this.form.reset({
      passDate: g.passDate,
      passTime: g.passTime,
      type: g.type, direction: g.direction,
      vehicleNumber: g.vehicleNumber ?? '', driverName: g.driverName ?? '',
      driverPhone: g.driverPhone ?? '', driverNidNumber: g.driverNidNumber ?? '',
      transporterName: g.transporterName ?? '',
      visitorName: g.visitorName ?? '', visitorPhone: g.visitorPhone ?? '',
      visitorOrganization: g.visitorOrganization ?? '', visitorPurpose: g.visitorPurpose ?? '',
      itemDescription: g.itemDescription ?? '', quantity: g.quantity ?? '',
      fromLocation: g.fromLocation ?? '', toLocation: g.toLocation ?? '',
      sourceType: g.sourceType ?? '', sourceCode: g.sourceCode ?? '',
      approvedByUser: g.approvedByUser ?? '',
      expectedReturnDate: g.expectedReturnDate,
      notes: g.notes ?? ''
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
      passDate: v.passDate,
      passTime: v.passTime || null,
      type: v.type, direction: v.direction,
      vehicleNumber: T(v.vehicleNumber),
      driverName: T(v.driverName),
      driverPhone: T(v.driverPhone),
      driverNidNumber: T(v.driverNidNumber),
      transporterName: T(v.transporterName),
      visitorName: T(v.visitorName),
      visitorPhone: T(v.visitorPhone),
      visitorOrganization: T(v.visitorOrganization),
      visitorPurpose: T(v.visitorPurpose),
      itemDescription: T(v.itemDescription),
      quantity: T(v.quantity),
      fromLocation: T(v.fromLocation),
      toLocation: T(v.toLocation),
      sourceType: T(v.sourceType),
      sourceId: null,
      sourceCode: T(v.sourceCode),
      approvedByUser: T(v.approvedByUser),
      expectedReturnDate: v.expectedReturnDate || null,
      notes: T(v.notes)
    };
    const obs: any = this.dialogMode === 'create'
      ? this.service.create(body)
      : this.service.update(this.editing!.id, body);
    obs.subscribe({
      next: (res: any) => this.zone.run(() => {
        this.dialogSaving = false;
        if (res.success) {
          this.dialogVisible = false;
          this.actionMessage = res.message || 'Saved.';
          this.load();
        } else {
          this.dialogError = res.message || 'Save failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err: any) => this.zone.run(() => {
        this.dialogSaving = false;
        this.dialogError = err?.error?.message || 'Save failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Close / Cancel / Delete ─────────────────────────────────────────────

  doAction(g: GatePassDto, action: 'close' | 'cancel' | 'delete'): void {
    const labels: Record<string, string> = {
      close: 'Close this gate pass?',
      cancel: 'Cancel this gate pass?',
      delete: 'Delete this Open gate pass? (soft delete)'
    };
    if (!confirm(labels[action])) return;
    if (this.rowActionId) return;
    this.rowActionId = g.id;
    this.actionError = '';
    this.cdr.detectChanges();
    const obs = action === 'close' ? this.service.close(g.id)
              : action === 'cancel' ? this.service.cancel(g.id)
              : this.service.delete(g.id);
    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.rowActionId = null;
        if (res.success) { this.actionMessage = res.message || 'Done.'; this.load(); }
        else this.actionError = res.message || 'Action failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.rowActionId = null;
        this.actionError = err?.error?.message || 'Action failed.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Mark Returned ───────────────────────────────────────────────────────

  openReturn(g: GatePassDto): void {
    this.returnTarget = g;
    this.returnNotes = '';
    this.returnError = '';
    this.returnVisible = true;
  }

  doReturn(): void {
    if (!this.returnTarget || this.returning) return;
    this.returning = true;
    this.returnError = '';
    this.cdr.detectChanges();
    this.service.markReturned(this.returnTarget.id, this.returnNotes?.trim() || null).subscribe({
      next: (res) => this.zone.run(() => {
        this.returning = false;
        if (res.success) { this.returnVisible = false; this.actionMessage = res.message || 'Returned.'; this.load(); }
        else this.returnError = res.message || 'Action failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.returning = false;
        this.returnError = err?.error?.message || 'Action failed.';
        this.cdr.detectChanges();
      })
    });
  }
}

function T(s: string | null | undefined): string | null {
  if (s === null || s === undefined) return null;
  const t = String(s).trim();
  return t.length === 0 ? null : t;
}
