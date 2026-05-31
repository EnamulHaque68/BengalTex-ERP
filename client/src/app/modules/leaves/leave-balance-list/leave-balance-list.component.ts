import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { LeavesService } from '../../../services/leaves.service';
import { EmployeeService } from '../../../services/employee.service';
import { AuthService } from '../../../services/auth.service';
import { LeaveBalanceDto } from '../../../models/leaves.models';

@Component({
  selector: 'app-leave-balance-list',
  standalone: false,
  templateUrl: './leave-balance-list.component.html',
  styleUrl: './leave-balance-list.component.scss'
})
export class LeaveBalanceListComponent implements OnInit {
  balances: LeaveBalanceDto[] = [];
  loading = false;
  year: number = new Date().getFullYear();
  filterEmployeeId: number | null = null;
  employees: any[] = [];

  initBusy = false;
  initMessage = '';

  canInit = false;
  canAdjust = false;

  adjustDialogVisible = false;
  adjustTarget: LeaveBalanceDto | null = null;
  adjustEntitled = 0;
  adjustTaken = 0;
  adjustBusy = false;
  adjustError = '';

  constructor(private svc: LeavesService, private empService: EmployeeService,
              private auth: AuthService, private zone: NgZone, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.canInit = this.auth.hasPermission('Leaves.ManageBalances');
    this.canAdjust = this.canInit;
    this.empService.getAll({ page: 1, pageSize: 1000, search: '' }, false).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.employees = res.data.items; this.cdr.detectChanges(); })
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getBalances(this.year, this.filterEmployeeId ?? undefined).subscribe({
      next: (res) => this.zone.run(() => { this.loading = false; if (res.success && res.data) this.balances = res.data; this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  initializeYear(): void {
    if (this.initBusy) return;
    this.initBusy = true; this.initMessage = ''; this.cdr.detectChanges();
    this.svc.initializeYear(this.year).subscribe({
      next: (res) => this.zone.run(() => { this.initBusy = false; if (res.success) { this.initMessage = res.message || 'Initialized.'; this.load(); } this.cdr.detectChanges(); }),
      error: () => this.zone.run(() => { this.initBusy = false; this.cdr.detectChanges(); })
    });
  }

  openAdjust(b: LeaveBalanceDto): void {
    this.adjustTarget = b; this.adjustEntitled = b.entitled; this.adjustTaken = b.taken; this.adjustError = '';
    this.adjustDialogVisible = true;
  }
  saveAdjust(): void {
    if (!this.adjustTarget || this.adjustBusy) return;
    this.adjustBusy = true; this.adjustError = ''; this.cdr.detectChanges();
    this.svc.adjustBalance(this.adjustTarget.id, Number(this.adjustEntitled) || 0, Number(this.adjustTaken) || 0).subscribe({
      next: (res) => this.zone.run(() => { this.adjustBusy = false; if (res.success) { this.adjustDialogVisible = false; this.adjustTarget = null; this.load(); } else this.adjustError = res.message || 'Save failed.'; this.cdr.detectChanges(); }),
      error: (e) => this.zone.run(() => { this.adjustBusy = false; this.adjustError = e?.error?.message || 'Save failed.'; this.cdr.detectChanges(); })
    });
  }
}
