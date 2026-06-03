import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';

const routes: Routes = [
  { path: 'login', loadChildren: () => import('./modules/auth/auth.module').then(m => m.AuthModule) },
  {
    path: '',
    canActivate: [AuthGuard],
    children: [
      { path: '', loadChildren: () => import('./modules/home/home.module').then(m => m.HomeModule) },
      { path: 'company', loadChildren: () => import('./modules/company/company.module').then(m => m.CompanyModule) },
      { path: 'factories', loadChildren: () => import('./modules/factory/factory.module').then(m => m.FactoryModule) },
      { path: 'users', loadChildren: () => import('./modules/user/user.module').then(m => m.UserModule) },
      { path: 'roles', loadChildren: () => import('./modules/role/role.module').then(m => m.RoleModule) },
      { path: 'currencies', loadChildren: () => import('./modules/currency/currency.module').then(m => m.CurrencyModule) },
      { path: 'units-of-measure', loadChildren: () => import('./modules/unit-of-measure/unit-of-measure.module').then(m => m.UnitOfMeasureModule) },
      { path: 'warehouses', loadChildren: () => import('./modules/warehouse/warehouse.module').then(m => m.WarehouseModule) },
      { path: 'customers', loadChildren: () => import('./modules/customer/customer.module').then(m => m.CustomerModule) },
      { path: 'suppliers', loadChildren: () => import('./modules/supplier/supplier.module').then(m => m.SupplierModule) },
      { path: 'product-categories', loadChildren: () => import('./modules/product-category/product-category.module').then(m => m.ProductCategoryModule) },
      { path: 'products', loadChildren: () => import('./modules/product/product.module').then(m => m.ProductModule) },
      { path: 'raw-materials', loadChildren: () => import('./modules/raw-material/raw-material.module').then(m => m.RawMaterialModule) },
      { path: 'boms', loadChildren: () => import('./modules/bom/bom.module').then(m => m.BomModule) },
      { path: 'purchase-requisitions', loadChildren: () => import('./modules/purchase-requisition/purchase-requisition.module').then(m => m.PurchaseRequisitionModule) },
      { path: 'purchase-orders', loadChildren: () => import('./modules/purchase-order/purchase-order.module').then(m => m.PurchaseOrderModule) },
      { path: 'gate-passes', loadChildren: () => import('./modules/gate-pass/gate-pass.module').then(m => m.GatePassModule) },
      { path: 'goods-receipts', loadChildren: () => import('./modules/goods-receipt/goods-receipt.module').then(m => m.GoodsReceiptModule) },
      { path: 'sales-orders', loadChildren: () => import('./modules/sales-order/sales-order.module').then(m => m.SalesOrderModule) },
      { path: 'stock-on-hand', loadChildren: () => import('./modules/stock-on-hand/stock-on-hand.module').then(m => m.StockOnHandModule) },
      { path: 'stock-movements', loadChildren: () => import('./modules/stock-movements/stock-movements.module').then(m => m.StockMovementsModule) },
      { path: 'stock-adjustments', loadChildren: () => import('./modules/stock-adjustment/stock-adjustment.module').then(m => m.StockAdjustmentModule) },
      { path: 'production-orders', loadChildren: () => import('./modules/production-order/production-order.module').then(m => m.ProductionOrderModule) },
      { path: 'delivery-notes', loadChildren: () => import('./modules/delivery-note/delivery-note.module').then(m => m.DeliveryNoteModule) },
      { path: 'customer-invoices', loadChildren: () => import('./modules/customer-invoice/customer-invoice.module').then(m => m.CustomerInvoiceModule) },
      { path: 'receipts', loadChildren: () => import('./modules/receipt/receipt.module').then(m => m.ReceiptModule) },
      { path: 'supplier-invoices', loadChildren: () => import('./modules/supplier-invoice/supplier-invoice.module').then(m => m.SupplierInvoiceModule) },
      { path: 'payments', loadChildren: () => import('./modules/payment/payment.module').then(m => m.PaymentModule) },
      { path: 'reports', loadChildren: () => import('./modules/reports/reports.module').then(m => m.ReportsModule) },
      { path: 'stock-transfers', loadChildren: () => import('./modules/stock-transfer/stock-transfer.module').then(m => m.StockTransferModule) },
      { path: 'vat-challans', loadChildren: () => import('./modules/vat-challan/vat-challan.module').then(m => m.VatChallanModule) },
      { path: 'customer-returns', loadChildren: () => import('./modules/customer-return-note/customer-return-note.module').then(m => m.CustomerReturnNoteModule) },
      { path: 'supplier-returns', loadChildren: () => import('./modules/supplier-return-note/supplier-return-note.module').then(m => m.SupplierReturnNoteModule) },
      { path: 'qc-inspections', loadChildren: () => import('./modules/qc-inspection/qc-inspection.module').then(m => m.QcInspectionModule) },
      { path: 'quarantine-dispositions', loadChildren: () => import('./modules/quarantine-disposition/quarantine-disposition.module').then(m => m.QuarantineDispositionModule) },
      { path: 'audit-log', loadChildren: () => import('./modules/audit-log/audit-log.module').then(m => m.AuditLogModule) },
      { path: 'approvals', loadChildren: () => import('./modules/approvals/approvals.module').then(m => m.ApprovalsModule) },
      { path: 'employees', loadChildren: () => import('./modules/employee/employee.module').then(m => m.EmployeeModule) },
      { path: 'attendance', loadChildren: () => import('./modules/attendance/attendance.module').then(m => m.AttendanceModule) },
      { path: 'payroll', loadChildren: () => import('./modules/payroll/payroll.module').then(m => m.PayrollModule) },
      { path: 'final-settlements', loadChildren: () => import('./modules/final-settlement/final-settlement.module').then(m => m.FinalSettlementModule) },
      { path: 'proforma-invoices', loadChildren: () => import('./modules/proforma-invoice/proforma-invoice.module').then(m => m.ProformaInvoiceModule) },
      { path: 'credit-notes', loadChildren: () => import('./modules/credit-note/credit-note.module').then(m => m.CreditNoteModule) },
      { path: 'debit-notes', loadChildren: () => import('./modules/debit-note/debit-note.module').then(m => m.DebitNoteModule) },
      { path: 'subcontract-orders', loadChildren: () => import('./modules/subcontract/subcontract.module').then(m => m.SubcontractModule) },
      { path: 'styles', loadChildren: () => import('./modules/style/style.module').then(m => m.StyleModule) },
      { path: 'letters-of-credit', loadChildren: () => import('./modules/banking/banking.module').then(m => m.BankingModule) },
      { path: 'notifications', loadChildren: () => import('./modules/notifications/notifications.module').then(m => m.NotificationsModule) },
      { path: 'stock-lots', loadChildren: () => import('./modules/stock-lots/stock-lots.module').then(m => m.StockLotsModule) },
      { path: 'accounting', loadChildren: () => import('./modules/accounting/accounting.module').then(m => m.AccountingModule) },
      { path: 'expenses', loadChildren: () => import('./modules/expenses/expenses.module').then(m => m.ExpensesModule) },
      { path: 'quotations', loadChildren: () => import('./modules/quotations/quotations.module').then(m => m.QuotationsModule) },
      { path: 'samples', loadChildren: () => import('./modules/samples/samples.module').then(m => m.SamplesModule) },
      { path: 'wastage', loadChildren: () => import('./modules/wastage/wastage.module').then(m => m.WastageModule) },
      { path: 'job-cards', loadChildren: () => import('./modules/job-cards/job-cards.module').then(m => m.JobCardsModule) },
      { path: 'leaves', loadChildren: () => import('./modules/leaves/leaves.module').then(m => m.LeavesModule) },
      { path: 'loans-bonuses', loadChildren: () => import('./modules/loans-bonuses/loans-bonuses.module').then(m => m.LoansBonusesModule) },
      { path: 'compliance', loadChildren: () => import('./modules/compliance/compliance.module').then(m => m.ComplianceModule) },
      { path: 'master-setup', loadChildren: () => import('./modules/master-setup/master-setup.module').then(m => m.MasterSetupModule) },
      { path: 'bank-reconciliation', loadChildren: () => import('./modules/bank-reconciliation/bank-reconciliation.module').then(m => m.BankReconciliationModule) },
      { path: 'dashboard', loadChildren: () => import('./modules/dashboard/dashboard.module').then(m => m.DashboardModule) }
    ]
  },
  { path: '**', redirectTo: '/login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
