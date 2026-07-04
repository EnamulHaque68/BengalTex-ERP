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
import { CheckboxModule } from 'primeng/checkbox';

import { CustomerInvoiceListComponent } from './customer-invoice-list/customer-invoice-list.component';
import { CustomerInvoicePrintComponent } from './customer-invoice-print/customer-invoice-print.component';
import { CommercialInvoicePrintComponent } from './commercial-invoice-print/commercial-invoice-print.component';
import { PackingListPrintComponent } from './packing-list-print/packing-list-print.component';
import { SharedModule } from '../../shared/shared.module';

const routes: Routes = [
  { path: '', component: CustomerInvoiceListComponent },
  { path: ':id/print', component: CustomerInvoicePrintComponent },
  { path: ':id/print-commercial', component: CommercialInvoicePrintComponent },
  { path: ':id/print-packing', component: PackingListPrintComponent }
];

@NgModule({
  declarations: [
    CustomerInvoiceListComponent,
    CustomerInvoicePrintComponent,
    CommercialInvoicePrintComponent,
    PackingListPrintComponent
  ],
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, TextareaModule, SelectModule,
    TooltipModule, CheckboxModule, SharedModule
  ]
})
export class CustomerInvoiceModule {}
