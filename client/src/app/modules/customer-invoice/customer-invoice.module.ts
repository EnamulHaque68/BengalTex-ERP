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

import { CustomerInvoiceListComponent } from './customer-invoice-list/customer-invoice-list.component';
import { CustomerInvoicePrintComponent } from './customer-invoice-print/customer-invoice-print.component';
import { SharedModule } from '../../shared/shared.module';

const routes: Routes = [
  { path: '', component: CustomerInvoiceListComponent },
  { path: ':id/print', component: CustomerInvoicePrintComponent }
];

@NgModule({
  declarations: [CustomerInvoiceListComponent, CustomerInvoicePrintComponent],
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, TextareaModule, SelectModule,
    TooltipModule, SharedModule
  ]
})
export class CustomerInvoiceModule {}
