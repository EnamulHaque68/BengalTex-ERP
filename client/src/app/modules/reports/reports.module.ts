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
import { VatSummaryComponent } from './vat-summary/vat-summary.component';
import { MarginReportComponent } from './margin-report/margin-report.component';
import { WipReportComponent } from './wip-report/wip-report.component';
import { ProductionSummaryComponent } from './production-summary/production-summary.component';
import { ProductivityReportComponent } from './productivity-report/productivity-report.component';
import { BuyerOrderBookComponent } from './buyer-order-book/buyer-order-book.component';
import { EpbExportRegisterComponent } from './epb-export-register/epb-export-register.component';
import { CustomerStatementComponent } from './customer-statement/customer-statement.component';
import { SupplierStatementComponent } from './supplier-statement/supplier-statement.component';

const routes: Routes = [
  { path: '', redirectTo: 'stock-summary', pathMatch: 'full' },
  { path: 'stock-summary', component: StockSummaryComponent },
  { path: 'ar-ageing', component: ArAgeingComponent },
  { path: 'ap-ageing', component: ApAgeingComponent },
  { path: 'sales-summary', component: SalesSummaryComponent },
  { path: 'vat-summary', component: VatSummaryComponent },
  { path: 'margin', component: MarginReportComponent },
  { path: 'wip', component: WipReportComponent },
  { path: 'production-summary', component: ProductionSummaryComponent },
  { path: 'productivity', component: ProductivityReportComponent },
  { path: 'buyer-order-book', component: BuyerOrderBookComponent },
  { path: 'epb-export-register', component: EpbExportRegisterComponent },
  { path: 'customer-statement', component: CustomerStatementComponent },
  { path: 'supplier-statement', component: SupplierStatementComponent }
];

@NgModule({
  declarations: [
    StockSummaryComponent,
    ArAgeingComponent,
    ApAgeingComponent,
    SalesSummaryComponent,
    VatSummaryComponent,
    MarginReportComponent,
    WipReportComponent,
    ProductionSummaryComponent,
    ProductivityReportComponent,
    BuyerOrderBookComponent,
    EpbExportRegisterComponent,
    CustomerStatementComponent,
    SupplierStatementComponent
  ],
  imports: [
    CommonModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule,
    InputTextModule, SelectModule, TooltipModule
  ]
})
export class ReportsModule {}
