import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';

import { AuditLogListComponent } from './audit-log-list/audit-log-list.component';

const routes: Routes = [{ path: '', component: AuditLogListComponent }];

@NgModule({
  declarations: [AuditLogListComponent],
  imports: [
    CommonModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, SelectModule, TooltipModule
  ]
})
export class AuditLogModule {}
