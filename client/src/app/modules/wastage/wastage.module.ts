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

import { WastageEntryListComponent } from './wastage-entry-list/wastage-entry-list.component';
import { WastageReasonListComponent } from './wastage-reason-list/wastage-reason-list.component';
import { WastageSummaryComponent } from './wastage-summary/wastage-summary.component';

const routes: Routes = [
  { path: '', component: WastageEntryListComponent },
  { path: 'reasons', component: WastageReasonListComponent },
  { path: 'summary', component: WastageSummaryComponent }
];

@NgModule({
  declarations: [WastageEntryListComponent, WastageReasonListComponent, WastageSummaryComponent],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, SelectModule, CheckboxModule, TooltipModule
  ]
})
export class WastageModule {}
