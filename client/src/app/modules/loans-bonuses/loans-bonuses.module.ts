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

import { EmployeeLoanListComponent } from './employee-loan-list/employee-loan-list.component';
import { FestivalBonusListComponent } from './festival-bonus-list/festival-bonus-list.component';

const routes: Routes = [
  { path: '', redirectTo: 'loans', pathMatch: 'full' },
  { path: 'loans', component: EmployeeLoanListComponent },
  { path: 'festival-bonuses', component: FestivalBonusListComponent }
];

@NgModule({
  declarations: [EmployeeLoanListComponent, FestivalBonusListComponent],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, SelectModule, TooltipModule,
    TextareaModule, TagModule
  ]
})
export class LoansBonusesModule {}
