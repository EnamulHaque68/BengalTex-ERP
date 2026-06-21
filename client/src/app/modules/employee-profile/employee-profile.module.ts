import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';

import { SharedModule } from '../../shared/shared.module';
import { EmployeeProfileComponent } from './employee-profile/employee-profile.component';

const routes: Routes = [
  { path: 'me', component: EmployeeProfileComponent, data: { mode: 'me' } },
  { path: ':id', component: EmployeeProfileComponent, data: { mode: 'id' } }
];

@NgModule({
  declarations: [EmployeeProfileComponent],
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule,
    RouterModule.forChild(routes),
    SharedModule,
    CardModule, ButtonModule, DialogModule,
    InputTextModule, TextareaModule, SelectModule, TooltipModule
  ]
})
export class EmployeeProfileModule {}
