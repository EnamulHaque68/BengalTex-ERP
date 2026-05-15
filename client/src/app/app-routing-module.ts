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
      { path: 'purchase-orders', loadChildren: () => import('./modules/purchase-order/purchase-order.module').then(m => m.PurchaseOrderModule) },
      { path: 'goods-receipts', loadChildren: () => import('./modules/goods-receipt/goods-receipt.module').then(m => m.GoodsReceiptModule) },
      { path: 'sales-orders', loadChildren: () => import('./modules/sales-order/sales-order.module').then(m => m.SalesOrderModule) },
      { path: 'stock-on-hand', loadChildren: () => import('./modules/stock-on-hand/stock-on-hand.module').then(m => m.StockOnHandModule) },
      { path: 'stock-movements', loadChildren: () => import('./modules/stock-movements/stock-movements.module').then(m => m.StockMovementsModule) },
      { path: 'stock-adjustments', loadChildren: () => import('./modules/stock-adjustment/stock-adjustment.module').then(m => m.StockAdjustmentModule) }
    ]
  },
  { path: '**', redirectTo: '/login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
