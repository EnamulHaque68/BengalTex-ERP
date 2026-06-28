import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';

import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';

import { ProductionCalendarComponent } from './production-calendar/production-calendar.component';

const routes: Routes = [
  { path: '', component: ProductionCalendarComponent }
];

@NgModule({
  declarations: [ProductionCalendarComponent],
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    ButtonModule, TooltipModule
  ]
})
export class ProductionCalendarModule {}
