import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';

import { VatChallanListComponent } from './vat-challan-list/vat-challan-list.component';
import { VatChallanPrintComponent } from './vat-challan-print/vat-challan-print.component';

const routes: Routes = [
  { path: '', component: VatChallanListComponent },
  { path: ':id/print', component: VatChallanPrintComponent }
];

@NgModule({
  declarations: [VatChallanListComponent, VatChallanPrintComponent],
  imports: [
    CommonModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, SelectModule, TooltipModule
  ]
})
export class VatChallanModule {}
