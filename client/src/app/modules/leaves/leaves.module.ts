import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { TooltipModule } from 'primeng/tooltip';
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { SharedModule } from '../../shared/shared.module';
import { LeaveListComponent } from './leave-list/leave-list.component';
import { LeaveTypeListComponent } from './leave-type-list/leave-type-list.component';
import { HolidayListComponent } from './holiday-list/holiday-list.component';
import { LeaveBalanceListComponent } from './leave-balance-list/leave-balance-list.component';

const routes: Routes = [
  { path: '', component: LeaveListComponent },
  { path: 'types', component: LeaveTypeListComponent },
  { path: 'holidays', component: HolidayListComponent },
  { path: 'balances', component: LeaveBalanceListComponent }
];

@NgModule({
  declarations: [LeaveListComponent, LeaveTypeListComponent, HolidayListComponent, LeaveBalanceListComponent],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, SelectModule, CheckboxModule, TooltipModule,
    TextareaModule, TagModule, ToggleSwitchModule,
    SharedModule
  ]
})
export class LeavesModule {}
