import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SampleService } from '../../../services/sample.service';
import { CustomerService } from '../../../services/customer.service';
import { ProductService } from '../../../services/product.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { SAMPLE_STATUSES, SampleDto, SampleListItemDto } from '../../../models/sample.models';

@Component({
  selector: 'app-sample-list',
  standalone: false,
  templateUrl: './sample-list.component.html',
  styleUrl: './sample-list.component.scss'
})
export class SampleListComponent implements OnInit {
  samples: SampleListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterStatus: string | null = null;
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;
  actionError = '';
  rowActionId: number | null = null;

  readonly statuses = SAMPLE_STATUSES;
  customers: any[] = [];
  products: any[] = [];

  canCreate = false;
  canManage = false;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  loaded: SampleDto | null = null;
  form!: FormGroup;

  decideDialogVisible = false;
  decideSample: SampleListItemDto | null = null;
  decideApprove = true;
  decideFeedback = '';
  decideBusy = false;
  decideError = '';

  deleteDialogVisible = false;
  deleting: SampleListItemDto | null = null;
  deleteBusy = false;
  deleteError = '';

  constructor(
    private svc: SampleService,
    private customerService: CustomerService,
    private productService: ProductService,
    private auth: AuthService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canCreate = this.auth.hasPermission('Samples.Create');
    this.canManage = this.auth.hasPermission('Samples.Manage');
    this.form = this.fb.group({
      customerId: [null as number | null, Validators.required],
      productId: [null as number | null],
      title: ['', [Validators.required, Validators.maxLength(200)]],
      buyerReference: ['', Validators.maxLength(100)],
      quantity: [1, Validators.min(0)],
      requestedDate: [this.todayIso(), Validators.required],
      targetDate: [null as string | null],
      description: ['', Validators.maxLength(2000)],
      notes: ['', Validators.maxLength(2000)]
    });
    this.loadDropdowns();
    this.load();
  }

  private todayIso(): string { return new Date().toISOString().slice(0, 10); }

  private loadDropdowns(): void {
    this.customerService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.customers = res.data.items; this.cdr.detectChanges(); })
    });
    this.productService.getAll({ page: 1, pageSize: 500, search: '' }, undefined, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.products = res.data.items; this.cdr.detectChanges(); })
    });
  }

  load(): void {
    this.loading = true;
    this.svc.getAll(this.parameters, undefined, this.filterStatus ?? undefined).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.samples = res.data.items; this.totalCount = res.data.totalCount; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearch(v: string): void { if (this.searchTimer) clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => { this.parameters.search = v; this.parameters.page = 1; this.load(); }, 350); }
  onPage(e: any): void { this.parameters.page = Math.floor(e.first / e.rows) + 1; this.parameters.pageSize = e.rows; this.load(); }

  openCreate(): void {
    this.dialogMode = 'create'; this.editingId = null; this.dialogError = ''; this.loaded = null;
    this.form.reset({ customerId: null, productId: null, title: '', buyerReference: '', quantity: 1, requestedDate: this.todayIso(), targetDate: null, description: '', notes: '' });
    this.form.enable();
    this.dialogVisible = true;
  }

  open(s: SampleListItemDto): void {
    this.editingId = s.id; this.dialogError = ''; this.dialogVisible = true; this.form.enable();
    this.svc.getById(s.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const x = res.data; this.loaded = x;
          this.dialogMode = (x.status === 'Approved' || x.status === 'Rejected') ? 'view' : 'edit';
          this.form.patchValue({
            customerId: x.customerId, productId: x.productId, title: x.title, buyerReference: x.buyerReference ?? '',
            quantity: x.quantity, requestedDate: x.requestedDate, targetDate: x.targetDate, description: x.description ?? '', notes: x.notes ?? ''
          });
          this.form.get('customerId')?.disable();   // customer fixed once created
          if (this.dialogMode === 'view') this.form.disable();
          this.cdr.detectChanges();
        }
      })
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving || this.dialogMode === 'view') return;
    this.dialogSaving = true; this.dialogError = ''; this.cdr.detectChanges();
    const v = this.form.getRawValue();
    const base = {
      productId: v.productId ?? null, styleId: null as number | null,
      title: (v.title as string).trim(), description: (v.description as string)?.trim() || null,
      buyerReference: (v.buyerReference as string)?.trim() || null, quantity: Number(v.quantity) || 0,
      requestedDate: v.requestedDate, targetDate: v.targetDate || null, notes: (v.notes as string)?.trim() || null
    };
    const done = (res: any) => this.zone.run(() => {
      this.dialogSaving = false;
      if (res.success) { if (this.dialogMode === 'create' && res.data) { this.loaded = res.data; this.editingId = res.data.id; this.dialogMode = 'edit'; } this.load(); }
      else this.dialogError = res.message || 'Save failed.';
      this.cdr.detectChanges();
    });
    const err = (e: any) => this.zone.run(() => { this.dialogSaving = false; this.dialogError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); });
    if (this.dialogMode === 'create') this.svc.create({ customerId: v.customerId, ...base }).subscribe({ next: done, error: err });
    else this.svc.update(this.editingId!, { id: this.editingId!, customerId: v.customerId, ...base }).subscribe({ next: done, error: err });
  }

  startDev(s: SampleListItemDto): void { this.rowAction(s, this.svc.startDevelopment(s.id)); }
  submit(s: SampleListItemDto): void { this.rowAction(s, this.svc.submit(s.id)); }
  private rowAction(s: SampleListItemDto, obs: any): void {
    if (this.rowActionId) return;
    this.rowActionId = s.id; this.actionError = ''; this.cdr.detectChanges();
    obs.subscribe({
      next: (res: any) => this.zone.run(() => { this.rowActionId = null; if (res.success) this.load(); else this.actionError = res.message || 'Action failed.'; this.cdr.detectChanges(); }),
      error: (e: any) => this.zone.run(() => { this.rowActionId = null; this.actionError = e?.error?.message || 'Action failed.'; this.cdr.detectChanges(); })
    });
  }

  openDecide(s: SampleListItemDto, approve: boolean): void {
    this.decideSample = s; this.decideApprove = approve; this.decideFeedback = ''; this.decideError = ''; this.decideDialogVisible = true;
  }
  doDecide(): void {
    if (!this.decideSample || this.decideBusy) return;
    this.decideBusy = true; this.decideError = ''; this.cdr.detectChanges();
    this.svc.decide(this.decideSample.id, this.decideApprove, this.decideFeedback.trim() || null).subscribe({
      next: (res) => this.zone.run(() => { this.decideBusy = false; if (res.success) { this.decideDialogVisible = false; this.decideSample = null; this.load(); } else this.decideError = res.message || 'Failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.decideBusy = false; this.decideError = e?.error?.message || 'Failed.'; this.cdr.detectChanges(); })
    });
  }

  confirmDelete(s: SampleListItemDto): void { this.deleting = s; this.deleteError = ''; this.deleteDialogVisible = true; }
  doDelete(): void {
    if (!this.deleting || this.deleteBusy) return;
    this.deleteBusy = true; this.deleteError = ''; this.cdr.detectChanges();
    this.svc.delete(this.deleting.id).subscribe({
      next: (res) => this.zone.run(() => { this.deleteBusy = false; if (res.success) { this.deleteDialogVisible = false; this.deleting = null; this.load(); } else this.deleteError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.deleteBusy = false; this.deleteError = e?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }

  statusClass(s: string): string { return s.toLowerCase(); }
}
