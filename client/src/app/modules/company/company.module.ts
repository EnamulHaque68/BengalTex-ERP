import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

// PrimeNG
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';

import { CompanySettingsComponent } from './company-settings/company-settings.component';

const routes: Routes = [
  { path: '', component: CompanySettingsComponent }
];

@NgModule({
  declarations: [CompanySettingsComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule.forChild(routes),
    CardModule,
    InputTextModule,
    ButtonModule,
    SkeletonModule
  ]
})
export class CompanyModule {}
