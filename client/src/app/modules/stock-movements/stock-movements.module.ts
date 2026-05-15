import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';

import { StockMovementListComponent } from './stock-movement-list/stock-movement-list.component';

const routes: Routes = [{ path: '', component: StockMovementListComponent }];

@NgModule({
  declarations: [StockMovementListComponent],
  imports: [
    CommonModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, InputTextModule, SelectModule
  ]
})
export class StockMovementsModule {}
