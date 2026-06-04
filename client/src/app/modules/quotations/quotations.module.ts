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

import { QuotationListComponent } from './quotation-list/quotation-list.component';
import { QuotationPrintComponent } from './quotation-print/quotation-print.component';
import { SharedModule } from '../../shared/shared.module';

const routes: Routes = [
  { path: '', component: QuotationListComponent },
  { path: ':id/print', component: QuotationPrintComponent }
];

@NgModule({
  declarations: [QuotationListComponent, QuotationPrintComponent],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, SelectModule, TooltipModule, SharedModule
  ]
})
export class QuotationsModule {}
