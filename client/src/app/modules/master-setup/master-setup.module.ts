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
import { TooltipModule } from 'primeng/tooltip';
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { MasterSetupLandingComponent } from './master-setup-landing/master-setup-landing.component';
import { DepartmentListComponent } from './department-list/department-list.component';
import { DesignationListComponent } from './designation-list/designation-list.component';
import { ShiftListComponent } from './shift-list/shift-list.component';
import { BankAccountListComponent } from './bank-account-list/bank-account-list.component';

const routes: Routes = [
  { path: '', component: MasterSetupLandingComponent },
  { path: 'departments', component: DepartmentListComponent },
  { path: 'designations', component: DesignationListComponent },
  { path: 'shifts', component: ShiftListComponent },
  { path: 'bank-accounts', component: BankAccountListComponent }
];

@NgModule({
  declarations: [
    MasterSetupLandingComponent, DepartmentListComponent,
    DesignationListComponent, ShiftListComponent, BankAccountListComponent
  ],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, SelectModule, TooltipModule,
    TextareaModule, TagModule, ToggleSwitchModule
  ]
})
export class MasterSetupModule {}
