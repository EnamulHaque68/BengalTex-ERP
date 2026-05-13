import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

// PrimeNG
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';

import { RoleListComponent } from './role-list/role-list.component';

const routes: Routes = [
  { path: '', component: RoleListComponent }
];

@NgModule({
  declarations: [RoleListComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    RouterModule.forChild(routes),
    CardModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TooltipModule
  ]
})
export class RoleModule {}
