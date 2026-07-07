import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { AccountingService, CostingRateDto, COSTING_RATE_TYPES, COSTING_RATE_BASES } from '../../../services/accounting.service';
import { WorkCenterService } from '../../../services/work-center.service';
import { WorkCenterDto } from '../../../models/work-center.models';
import { AuthService } from '../../../services/auth.service';
import { ApiResponse } from '../../../models/auth.models';
import { apiErrorMessage } from '../../../shared/utils/http-error.util';

/**
 * Phase A4 — absorption costing rates. No rate configured = that conversion layer is not
 * absorbed = production stays material-only (pre-A4 behaviour). Setting rates activates the
 * F1 fix (fully-loaded product cost).
 */
@Component({
  selector: 'app-costing-rates',
  standalone: false,
  templateUrl: './costing-rates.component.html',
  styleUrl: './costing-rates.component.scss'
})
export class CostingRatesComponent implements OnInit {
  rates: CostingRateDto[] = [];
  workCenters: WorkCenterDto[] = [];
  loading = false;
  canManage = false;
  readonly types = COSTING_RATE_TYPES;
  readonly bases = COSTING_RATE_BASES;

  dialogVisible = false;
  saving = false;
  dialogError = '';
  editingId: number | null = null;
  rateType = 'Labour';
  basis = 'PerLabourMinute';
  rate = 0;
  effectiveFrom = '';
  workCenterId: number | null = null;
  notes = '';
  isActive = true;

  constructor(
    private svc: AccountingService,
    private wcSvc: WorkCenterService,
    private auth: AuthService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('Accounting.ManageDimensions');
    this.load();
    this.wcSvc.getAll(false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.workCenters = res.data; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getCostingRates(true).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.rates = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  basisFor(type: string): string {
    return type === 'Labour' ? 'PerLabourMinute' : type === 'MachineOH' ? 'PerMachineHour' : 'PerUnit';
  }
  onTypeChange(): void { this.basis = this.basisFor(this.rateType); }

  openCreate(): void {
    this.editingId = null; this.rateType = 'Labour'; this.basis = 'PerLabourMinute';
    this.rate = 0; this.effectiveFrom = new Date().toISOString().slice(0, 10);
    this.workCenterId = null; this.notes = ''; this.isActive = true; this.dialogError = '';
    this.dialogVisible = true;
  }

  openEdit(r: CostingRateDto): void {
    this.editingId = r.id; this.rateType = r.rateType; this.basis = r.basis; this.rate = r.rate;
    this.effectiveFrom = r.effectiveFrom; this.workCenterId = r.workCenterId; this.notes = r.notes ?? '';
    this.isActive = r.isActive; this.dialogError = ''; this.dialogVisible = true;
  }

  save(): void {
    if (this.saving || this.rate < 0 || !this.effectiveFrom) return;
    this.saving = true; this.dialogError = '';
    const body = {
      rateType: this.rateType, basis: this.basis, rate: Number(this.rate) || 0,
      effectiveFrom: this.effectiveFrom, workCenterId: this.workCenterId, notes: this.notes.trim() || null
    };
    const obs: Observable<ApiResponse<number | null>> = this.editingId
      ? this.svc.updateCostingRate(this.editingId, { ...body, isActive: this.isActive })
      : this.svc.createCostingRate(body);
    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.saving = false;
        if (res.success) { this.dialogVisible = false; this.load(); }
        else this.dialogError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.saving = false; this.dialogError = apiErrorMessage(err, 'Save failed.'); this.cdr.detectChanges(); })
    });
  }

  typeLabel(t: string): string { return t === 'MachineOH' ? 'Machine OH' : t === 'FactoryOH' ? 'Factory OH' : t; }
  typeClass(t: string): string { return t === 'Labour' ? 't-lab' : t === 'MachineOH' ? 't-mac' : 't-foh'; }
}
