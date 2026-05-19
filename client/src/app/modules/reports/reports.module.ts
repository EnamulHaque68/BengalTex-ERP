import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';

import { StockSummaryComponent } from './stock-summary/stock-summary.component';
import { ArAgeingComponent } from './ar-ageing/ar-ageing.component';
import { ApAgeingComponent } from './ap-ageing/ap-ageing.component';
import { SalesSummaryComponent } from './sales-summary/sales-summary.component';

const routes: Routes = [
  { path: '', redirectTo: 'stock-summary', pathMatch: 'full' },
  { path: 'stock-summary', component: StockSummaryComponent },
  { path: 'ar-ageing', component: ArAgeingComponent },
  { path: 'ap-ageing', component: ApAgeingComponent },
  { path: 'sales-summary', component: SalesSummaryComponent }
];

@NgModule({
  declarations: [
    StockSummaryComponent,
    ArAgeingComponent,
    ApAgeingComponent,
    SalesSummaryComponent
  ],
  imports: [
    CommonModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule,
    InputTextModule, SelectModule, TooltipModule
  ]
})
export class ReportsModule {}
