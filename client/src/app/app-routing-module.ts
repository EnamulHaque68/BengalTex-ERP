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
      { path: 'raw-materials', loadChildren: () => import('./modules/raw-material/raw-material.module').then(m => m.RawMaterialModule) }
    ]
  },
  { path: '**', redirectTo: '/login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
