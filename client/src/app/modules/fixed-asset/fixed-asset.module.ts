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

import { FixedAssetListComponent } from './fixed-asset-list/fixed-asset-list.component';
import { DepreciationRunListComponent } from './depreciation-run-list/depreciation-run-list.component';

const routes: Routes = [
  { path: '', redirectTo: 'assets', pathMatch: 'full' },
  { path: 'assets', component: FixedAssetListComponent },
  { path: 'depreciation-runs', component: DepreciationRunListComponent }
];

@NgModule({
  declarations: [FixedAssetListComponent, DepreciationRunListComponent],
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, TextareaModule, SelectModule, TooltipModule
  ]
})
export class FixedAssetModule {}
