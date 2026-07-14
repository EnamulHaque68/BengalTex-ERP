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
import { TextareaModule } from 'primeng/textarea';

import { SharedModule } from '../../shared/shared.module';

import { ChartOfAccountsComponent } from './chart-of-accounts/chart-of-accounts.component';
import { JournalEntryListComponent } from './journal-entry-list/journal-entry-list.component';
import { TrialBalanceComponent } from './trial-balance/trial-balance.component';
import { GeneralLedgerComponent } from './general-ledger/general-ledger.component';
import { ProfitLossComponent } from './profit-loss/profit-loss.component';
import { BalanceSheetComponent } from './balance-sheet/balance-sheet.component';
import { CashFlowComponent } from './cash-flow/cash-flow.component';
import { CashBookComponent } from './cash-book/cash-book.component';
import { BankBookComponent } from './bank-book/bank-book.component';
import { DayBookComponent } from './day-book/day-book.component';
import { FiscalYearListComponent } from './fiscal-year-list/fiscal-year-list.component';
import { OpeningBalancesComponent } from './opening-balances/opening-balances.component';
import { InventoryTieOutComponent } from './inventory-tie-out/inventory-tie-out.component';
import { CostCentersComponent } from './cost-centers/cost-centers.component';
import { ProfitabilityComponent } from './profitability/profitability.component';
import { CostingRatesComponent } from './costing-rates/costing-rates.component';
import { ProductionCostingComponent } from './production-costing/production-costing.component';
import { StatutoryLiabilitiesComponent } from './statutory-liabilities/statutory-liabilities.component';
import { ExchangeRatesComponent } from './exchange-rates/exchange-rates.component';
import { BudgetsComponent } from './budgets/budgets.component';
import { FinancialIntelligenceComponent } from './financial-intelligence/financial-intelligence.component';

const routes: Routes = [
  { path: 'accounts', component: ChartOfAccountsComponent },
  { path: 'journals', component: JournalEntryListComponent },
  { path: 'trial-balance', component: TrialBalanceComponent },
  { path: 'general-ledger', component: GeneralLedgerComponent },
  { path: 'profit-loss', component: ProfitLossComponent },
  { path: 'balance-sheet', component: BalanceSheetComponent },
  { path: 'cash-flow', component: CashFlowComponent },
  { path: 'cash-book', component: CashBookComponent },
  { path: 'bank-book', component: BankBookComponent },
  { path: 'day-book', component: DayBookComponent },
  // Phase A1 — fiscal rails
  { path: 'fiscal-years', component: FiscalYearListComponent },
  { path: 'opening-balances', component: OpeningBalancesComponent },
  // Phase A2 — inventory truth
  { path: 'inventory-tie-out', component: InventoryTieOutComponent },
  // Phase A3 — dimensions
  { path: 'cost-centers', component: CostCentersComponent },
  { path: 'profitability', component: ProfitabilityComponent },
  // Phase A4 — true cost
  { path: 'costing-rates', component: CostingRatesComponent },
  { path: 'production-costing', component: ProductionCostingComponent },
  // Phase A5b — statutory withholding
  { path: 'statutory-liabilities', component: StatutoryLiabilitiesComponent },
  // Phase A6c — dated exchange rates
  { path: 'exchange-rates', component: ExchangeRatesComponent },
  // Phase A7a — budgeting
  { path: 'budgets', component: BudgetsComponent },
  // Phase A8 — financial intelligence
  { path: 'financial-intelligence', component: FinancialIntelligenceComponent },
  { path: '', redirectTo: 'accounts', pathMatch: 'full' }
];

@NgModule({
  declarations: [
    ChartOfAccountsComponent, JournalEntryListComponent, TrialBalanceComponent,
    GeneralLedgerComponent, ProfitLossComponent, BalanceSheetComponent, CashFlowComponent,
    CashBookComponent, BankBookComponent, DayBookComponent,
    FiscalYearListComponent, OpeningBalancesComponent, InventoryTieOutComponent,
    CostCentersComponent, ProfitabilityComponent,
    CostingRatesComponent, ProductionCostingComponent,
    StatutoryLiabilitiesComponent, ExchangeRatesComponent, BudgetsComponent,
    FinancialIntelligenceComponent
  ],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, SelectModule, CheckboxModule, TooltipModule,
    TextareaModule,
    SharedModule
  ]
})
export class AccountingModule {}
