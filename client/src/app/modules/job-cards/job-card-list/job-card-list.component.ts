import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { JobCardService } from '../../../services/job-card.service';
import { ProductionOrderService } from '../../../services/production-order.service';
import { EmployeeService } from '../../../services/employee.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  JobCardListItemDto, JobCardBoardCountsDto, JobCardStatus, JOB_CARD_STATUSES, MachineDto
} from '../../../models/job-card.models';

@Component({
  selector: 'app-job-card-list',
  standalone: false,
  templateUrl: './job-card-list.component.html',
  styleUrl: './job-card-list.component.scss'
})
export class JobCardListComponent implements OnInit {

  cards: JobCardListItemDto[] = [];
  loading = false;
  totalCount = 0;
  counts: JobCardBoardCountsDto = { open: 0, inProgress: 0, onHold: 0, completed: 0, cancelled: 0 };
  filterStatus: JobCardStatus | null = null;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  machines: MachineDto[] = [];
  productionOrders: any[] = [];
  operators: any[] = [];

  readonly statuses = JOB_CARD_STATUSES;

  canCreate = false;
  canEdit = false;
  canDelete = false;
  canScan = false;

  dialogVisible = false;
  dialogSaving = false;
  dialogError = '';
  form!: FormGroup;

  scanVisible = false;
  scanBusy = false;
  scanError = '';
  scanTarget: JobCardListItemDto | null = null;
  scanForm!: FormGroup;
  readonly scanActions = [
    { label: 'Start',    value: 'Start',    severity: 'success'   },
    { label: 'Pause',    value: 'Pause',    severity: 'warning'   },
    { label: 'Resume',   value: 'Resume',   severity: 'info'      },
    { label: 'Complete', value: 'Complete', severity: 'success'   },
    { label: 'QC Check', value: 'QcCheck',  severity: 'secondary' },
    { label: 'Cancel',   value: 'Cancel',   severity: 'danger'    }
  ];

  constructor(
    private svc: JobCardService,
    private poService: ProductionOrderService,
    private empService: EmployeeService,
    private auth: AuthService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.canCreate = this.auth.hasPermission('JobCards.Create');
    this.canEdit = this.auth.hasPermission('JobCards.Edit');
    this.canDelete = this.auth.hasPermission('JobCards.Delete');
    this.canScan = this.auth.hasPermission('JobCards.Scan');

    this.form = this.fb.group({
      productionOrderId: [null as number | null, Validators.required],
      productionStageId: [null as number | null],
      batchNumber: ['', Validators.maxLength(100)],
      quantity: [1, [Validators.required, Validators.min(0.0001)]],
      machineId: [null as number | null],
      operatorEmployeeId: [null as number | null],
      notes: ['', Validators.maxLength(2000)]
    });
    this.scanForm = this.fb.group({
      scanType: ['Start', Validators.required],
      quantity: [null as number | null, [Validators.min(0)]],
      rejectedQuantity: [null as number | null, [Validators.min(0)]],
      notes: ['', Validators.maxLength(1000)]
    });

    this.loadDropdowns();
    this.load();
  }

  private loadDropdowns(): void {
    this.svc.getMachines(false).subscribe({ next: (res) => this.zone.run(() => { if (res.success && res.data) this.machines = res.data; this.cdr.detectChanges(); }) });
    this.poService.getAll({ page: 1, pageSize: 200, search: '' }).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.productionOrders = res.data.items; this.cdr.detectChanges(); })
    });
    this.empService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.operators = res.data.items; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters, this.filterStatus).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.cards = res.data.items; this.totalCount = res.data.totalCount; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
    this.svc.boardCounts().subscribe({ next: (res) => this.zone.run(() => { if (res.success && res.data) this.counts = res.data; this.cdr.detectChanges(); }) });
  }

  setStatusFilter(s: JobCardStatus | null): void { this.filterStatus = s; this.parameters.page = 1; this.load(); }

  onSearch(v: string): void { if (this.searchTimer) clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => { this.parameters.search = v; this.parameters.page = 1; this.load(); }, 350); }
  onPage(e: any): void { this.parameters.page = Math.floor(e.first / e.rows) + 1; this.parameters.pageSize = e.rows; this.load(); }

  openCreate(): void {
    this.dialogError = '';
    this.form.reset({ productionOrderId: null, productionStageId: null, batchNumber: '', quantity: 1, machineId: null, operatorEmployeeId: null, notes: '' });
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    this.svc.create({
      productionOrderId: v.productionOrderId,
      productionStageId: v.productionStageId,
      batchNumber: (v.batchNumber as string)?.trim() || null,
      quantity: Number(v.quantity) || 0,
      machineId: v.machineId, operatorEmployeeId: v.operatorEmployeeId,
      notes: (v.notes as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => { this.dialogSaving = false; if (res.success) { this.dialogVisible = false; this.load(); } else this.dialogError = res.message || 'Save failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); })
    });
  }

  view(c: JobCardListItemDto): void { this.router.navigate(['/job-cards', c.id]); }

  openScan(c: JobCardListItemDto): void {
    this.scanTarget = c; this.scanError = '';
    const nextAction = c.status === 'Open' ? 'Start' : c.status === 'InProgress' ? 'Pause' : c.status === 'OnHold' ? 'Resume' : 'QcCheck';
    this.scanForm.reset({ scanType: nextAction, quantity: c.status === 'InProgress' || c.status === 'OnHold' ? c.quantity : null, rejectedQuantity: null, notes: '' });
    this.scanVisible = true;
  }

  doScan(): void {
    if (!this.scanTarget || this.scanForm.invalid || this.scanBusy) return;
    this.scanBusy = true; this.scanError = ''; this.cdr.detectChanges();
    const v = this.scanForm.getRawValue();
    this.svc.scan({
      jobCardId: this.scanTarget.id, scanType: v.scanType,
      quantity: v.quantity, rejectedQuantity: v.rejectedQuantity,
      notes: (v.notes as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => { this.scanBusy = false; if (res.success) { this.scanVisible = false; this.scanTarget = null; this.load(); } else this.scanError = res.message || 'Scan failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.scanBusy = false; this.scanError = e?.error?.message || 'Scan failed.'; this.cdr.detectChanges(); })
    });
  }

  statusSeverity(s: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (s) {
      case 'Open': return 'secondary';
      case 'InProgress': return 'success';
      case 'OnHold': return 'warn';
      case 'Completed': return 'info';
      case 'Cancelled': return 'danger';
      default: return 'secondary';
    }
  }

  formatMinutes(m: number | null): string {
    if (m == null) return '—';
    if (m < 60) return `${m}m`;
    const h = Math.floor(m / 60), r = m % 60;
    return `${h}h ${r}m`;
  }
}
