import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';

import { LcListComponent } from './lc-list/lc-list.component';
import { ExportIncentiveListComponent } from './export-incentive-list/export-incentive-list.component';
import { BankFacilityListComponent } from './bank-facility-list/bank-facility-list.component';

const routes: Routes = [
  { path: '', component: LcListComponent },
  { path: 'export-incentives', component: ExportIncentiveListComponent },
  { path: 'bank-facilities', component: BankFacilityListComponent }
];

@NgModule({
  declarations: [LcListComponent, ExportIncentiveListComponent, BankFacilityListComponent],
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, TextareaModule, SelectModule, TooltipModule
  ]
})
export class BankingModule {}
