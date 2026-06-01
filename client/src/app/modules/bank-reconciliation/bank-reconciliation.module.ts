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
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';

import { BankStatementListComponent } from './bank-statement-list/bank-statement-list.component';
import { ReconciliationWorkspaceComponent } from './reconciliation-workspace/reconciliation-workspace.component';

const routes: Routes = [
  { path: '', component: BankStatementListComponent },
  { path: ':id', component: ReconciliationWorkspaceComponent }
];

@NgModule({
  declarations: [BankStatementListComponent, ReconciliationWorkspaceComponent],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, SelectModule, TooltipModule,
    TextareaModule, TagModule
  ]
})
export class BankReconciliationModule {}
