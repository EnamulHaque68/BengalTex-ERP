import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { ShopFloorComponent } from './shop-floor/shop-floor.component';

const routes: Routes = [
  { path: '', component: ShopFloorComponent }
];

@NgModule({
  declarations: [ShopFloorComponent],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule.forChild(routes)
  ]
})
export class ShopFloorModule {}
