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

import { PaymentListComponent } from './payment-list/payment-list.component';
import { PaymentPrintComponent } from './payment-print/payment-print.component';

const routes: Routes = [
  { path: '', component: PaymentListComponent },
  { path: ':id/print', component: PaymentPrintComponent }
];

@NgModule({
  declarations: [PaymentListComponent, PaymentPrintComponent],
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, TextareaModule, SelectModule,
    TooltipModule
  ]
})
export class PaymentModule {}
