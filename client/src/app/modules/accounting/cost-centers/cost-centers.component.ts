import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { AccountingService, CostCenterDto, COST_CENTER_KINDS } from '../../../services/accounting.service';
import { MasterSetupService } from '../../../services/master-setup.service';
import { FactoryService } from '../../../services/company.service';
import { DepartmentDto } from '../../../models/master-setup.models';
import { FactoryListItemDto } from '../../../models/company.models';
import { AuthService } from '../../../services/auth.service';
import { ApiResponse } from '../../../models/auth.models';
import { apiErrorMessage } from '../../../shared/utils/http-error.util';

/** Phase A3 — Cost / profit center master (the primary accounting dimension). */
@Component({
  selector: 'app-cost-centers',
  standalone: false,
  templateUrl: './cost-centers.component.html',
  styleUrl: './cost-centers.component.scss'
})
export class CostCentersComponent implements OnInit {
  centers: CostCenterDto[] = [];
  departments: DepartmentDto[] = [];
  factories: FactoryListItemDto[] = [];
  loading = false;
  error = '';
  canManage = false;
  readonly kinds = COST_CENTER_KINDS;

  dialogVisible = false;
  saving = false;
  dialogError = '';
  editingId: number | null = null;
  code = '';
  name = '';
  kind = 'Cost';
  parentCostCenterId: number | null = null;
  departmentId: number | null = null;
  factoryId: number | null = null;
  description = '';
  isActive = true;

  constructor(
    private svc: AccountingService,
    private masterSvc: MasterSetupService,
    private factorySvc: FactoryService,
    private auth: AuthService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canManage = this.auth.hasPermission('Accounting.ManageDimensions');
    this.load();
    this.masterSvc.getDepartments(false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.departments = res.data; this.cdr.detectChanges(); })
    });
    this.factorySvc.getAll(false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.factories = res.data; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getCostCenters(true).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.centers = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  get parentOptions(): CostCenterDto[] {
    return this.centers.filter(c => c.id !== this.editingId);
  }

  openCreate(): void {
    this.editingId = null; this.code = ''; this.name = ''; this.kind = 'Cost';
    this.parentCostCenterId = null; this.departmentId = null; this.factoryId = null;
    this.description = ''; this.isActive = true;
    this.dialogError = ''; this.dialogVisible = true;
  }

  openEdit(c: CostCenterDto): void {
    this.editingId = c.id; this.code = c.code; this.name = c.name; this.kind = c.kind;
    this.parentCostCenterId = c.parentCostCenterId;
    this.departmentId = c.departmentId; this.factoryId = c.factoryId;
    this.description = c.description ?? '';
    this.isActive = c.isActive; this.dialogError = ''; this.dialogVisible = true;
  }

  save(): void {
    if (this.saving || !this.name.trim() || (!this.editingId && !this.code.trim())) return;
    this.saving = true; this.dialogError = '';
    const body = {
      name: this.name.trim(), kind: this.kind, parentCostCenterId: this.parentCostCenterId,
      departmentId: this.departmentId, factoryId: this.factoryId, description: this.description.trim() || null
    };
    const obs: Observable<ApiResponse<number | null>> = this.editingId
      ? this.svc.updateCostCenter(this.editingId, { ...body, isActive: this.isActive })
      : this.svc.createCostCenter({ ...body, code: this.code.trim() });
    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.saving = false;
        if (res.success) { this.dialogVisible = false; this.load(); }
        else this.dialogError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.saving = false; this.dialogError = apiErrorMessage(err, 'Save failed.'); this.cdr.detectChanges();
      })
    });
  }

  kindClass(k: string): string {
    return k === 'Profit' ? 'k-profit' : k === 'Both' ? 'k-both' : 'k-cost';
  }
}
