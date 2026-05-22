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
import { CheckboxModule } from 'primeng/checkbox';
import { TooltipModule } from 'primeng/tooltip';

import { ExpenseListComponent } from './expense-list/expense-list.component';
import { ExpenseCategoryListComponent } from './expense-category-list/expense-category-list.component';
import { ExpenseSummaryComponent } from './expense-summary/expense-summary.component';

const routes: Routes = [
  { path: '', component: ExpenseListComponent },
  { path: 'categories', component: ExpenseCategoryListComponent },
  { path: 'summary', component: ExpenseSummaryComponent }
];

@NgModule({
  declarations: [ExpenseListComponent, ExpenseCategoryListComponent, ExpenseSummaryComponent],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, SelectModule, CheckboxModule, TooltipModule
  ]
})
export class ExpensesModule {}
