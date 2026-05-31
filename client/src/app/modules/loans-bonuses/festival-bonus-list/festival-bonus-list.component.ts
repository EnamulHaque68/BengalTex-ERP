import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LoanBonusService } from '../../../services/loan-bonus.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  FestivalBonusDto, FestivalBonusType, FestivalBonusStatus,
  FESTIVAL_BONUS_TYPES, FESTIVAL_BONUS_STATUSES
} from '../../../models/loan-bonus.models';

@Component({
  selector: 'app-festival-bonus-list',
  standalone: false,
  templateUrl: './festival-bonus-list.component.html',
  styleUrl: './festival-bonus-list.component.scss'
})
export class FestivalBonusListComponent implements OnInit {
  bonuses: FestivalBonusDto[] = [];
  loading = false;
  totalCount = 0;
  filterYear: number = new Date().getFullYear();
  filterType: FestivalBonusType | null = null;
  filterStatus: FestivalBonusStatus | null = null;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly bonusTypes = FESTIVAL_BONUS_TYPES;
  readonly statuses = FESTIVAL_BONUS_STATUSES;
  readonly paymentMethods = [
    { label: 'Bank Transfer', value: 'BankTransfer' },
    { label: 'Cash', value: 'Cash' },
    { label: 'Cheque', value: 'Cheque' },
    { label: 'Mobile Banking (bKash/Nagad)', value: 'MobileBanking' },
    { label: 'Other', value: 'Other' }
  ];

  canCreate = false;
  canEdit = false;
  canDelete = false;
  canPay = false;

  bulkVisible = false;
  bulkSaving = false;
  bulkError = '';
  bulkForm!: FormGroup;

  editVisible = false;
  editSaving = false;
  editError = '';
  editing: FestivalBonusDto | null = null;
  editForm!: FormGroup;

  rowActionId: number | null = null;
  actionMessage = '';

  constructor(private svc: LoanBonusService, private auth: AuthService,
              private fb: FormBuilder, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.canCreate = this.auth.hasPermission('FestivalBonuses.Create');
    this.canEdit = this.auth.hasPermission('FestivalBonuses.Edit');
    this.canDelete = this.auth.hasPermission('FestivalBonuses.Delete');
    this.canPay = this.auth.hasPermission('FestivalBonuses.Pay');

    this.bulkForm = this.fb.group({
      bonusYear: [new Date().getFullYear(), [Validators.required, Validators.min(2000), Validators.max(2100)]],
      bonusType: ['EidUlFitr' as FestivalBonusType, Validators.required],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      notes: ['', Validators.maxLength(1000)]
    });

    this.editForm = this.fb.group({
      amount: [0, [Validators.required, Validators.min(0.01)]],
      paymentMethod: ['BankTransfer', Validators.required],
      notes: ['', Validators.maxLength(1000)]
    });

    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getBonuses(this.parameters, this.filterYear, this.filterType, this.filterStatus).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.bonuses = res.data.items; this.totalCount = res.data.totalCount; }
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onSearch(v: string): void { if (this.searchTimer) clearTimeout(this.searchTimer); this.searchTimer = setTimeout(() => { this.parameters.search = v; this.parameters.page = 1; this.load(); }, 350); }
  onPage(e: any): void { this.parameters.page = Math.floor(e.first / e.rows) + 1; this.parameters.pageSize = e.rows; this.load(); }

  openBulk(): void {
    this.bulkError = '';
    this.bulkForm.reset({ bonusYear: this.filterYear, bonusType: 'EidUlFitr', amount: 0, notes: '' });
    this.bulkVisible = true;
  }
  doBulk(): void {
    if (this.bulkForm.invalid || this.bulkSaving) return;
    this.bulkSaving = true; this.bulkError = ''; this.cdr.detectChanges();
    const v = this.bulkForm.getRawValue();
    this.svc.bulkCreateBonus({
      bonusYear: Number(v.bonusYear) || new Date().getFullYear(),
      bonusType: v.bonusType,
      amount: Number(v.amount) || 0,
      notes: (v.notes as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => { this.bulkSaving = false; if (res.success) { this.bulkVisible = false; this.actionMessage = res.message || 'Bonuses created.'; this.load(); } else this.bulkError = res.message || 'Failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.bulkSaving = false; this.bulkError = e?.error?.message || 'Failed.'; this.cdr.detectChanges(); })
    });
  }

  openEdit(b: FestivalBonusDto): void {
    this.editing = b; this.editError = '';
    this.editForm.reset({ amount: b.amount, paymentMethod: b.paymentMethod || 'BankTransfer', notes: b.notes ?? '' });
    this.editVisible = true;
  }
  saveEdit(): void {
    if (!this.editing || this.editForm.invalid || this.editSaving) return;
    this.editSaving = true; this.editError = ''; this.cdr.detectChanges();
    const v = this.editForm.getRawValue();
    this.svc.updateBonus(this.editing.id, {
      amount: Number(v.amount) || 0, paymentMethod: v.paymentMethod,
      notes: (v.notes as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => { this.editSaving = false; if (res.success) { this.editVisible = false; this.editing = null; this.load(); } else this.editError = res.message || 'Save failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.editSaving = false; this.editError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); })
    });
  }

  pay(b: FestivalBonusDto): void {
    if (this.rowActionId) return;
    this.rowActionId = b.id; this.cdr.detectChanges();
    this.svc.pay(b.id).subscribe({
      next: () => this.zone.run(() => { this.rowActionId = null; this.load(); }),
      error: () => this.zone.run(() => { this.rowActionId = null; this.cdr.detectChanges(); })
    });
  }

  remove(b: FestivalBonusDto): void {
    if (this.rowActionId) return;
    this.rowActionId = b.id; this.cdr.detectChanges();
    this.svc.deleteBonus(b.id).subscribe({
      next: () => this.zone.run(() => { this.rowActionId = null; this.load(); }),
      error: () => this.zone.run(() => { this.rowActionId = null; this.cdr.detectChanges(); })
    });
  }

  statusSeverity(s: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    return s === 'Draft' ? 'warn' : s === 'Paid' ? 'success' : 'secondary';
  }

  bonusTypeLabel(t: string): string {
    return FESTIVAL_BONUS_TYPES.find(x => x.value === t)?.label ?? t;
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 2 }).format(amount || 0);
  }
}
