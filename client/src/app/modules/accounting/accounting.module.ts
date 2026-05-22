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

import { ChartOfAccountsComponent } from './chart-of-accounts/chart-of-accounts.component';
import { JournalEntryListComponent } from './journal-entry-list/journal-entry-list.component';
import { TrialBalanceComponent } from './trial-balance/trial-balance.component';
import { GeneralLedgerComponent } from './general-ledger/general-ledger.component';

const routes: Routes = [
  { path: 'accounts', component: ChartOfAccountsComponent },
  { path: 'journals', component: JournalEntryListComponent },
  { path: 'trial-balance', component: TrialBalanceComponent },
  { path: 'general-ledger', component: GeneralLedgerComponent },
  { path: '', redirectTo: 'accounts', pathMatch: 'full' }
];

@NgModule({
  declarations: [ChartOfAccountsComponent, JournalEntryListComponent, TrialBalanceComponent, GeneralLedgerComponent],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, SelectModule, CheckboxModule, TooltipModule
  ]
})
export class AccountingModule {}
