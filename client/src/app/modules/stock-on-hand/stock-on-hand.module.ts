import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ToggleButtonModule } from 'primeng/togglebutton';

import { StockOnHandListComponent } from './stock-on-hand-list/stock-on-hand-list.component';

const routes: Routes = [{ path: '', component: StockOnHandListComponent }];

@NgModule({
  declarations: [StockOnHandListComponent],
  imports: [
    CommonModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, InputTextModule, SelectModule, ToggleButtonModule
  ]
})
export class StockOnHandModule {}
