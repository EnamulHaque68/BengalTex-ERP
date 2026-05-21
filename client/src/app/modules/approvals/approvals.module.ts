import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { TextareaModule } from 'primeng/textarea';

import { ApprovalsListComponent } from './approvals-list/approvals-list.component';

const routes: Routes = [{ path: '', component: ApprovalsListComponent }];

@NgModule({
  declarations: [ApprovalsListComponent],
  imports: [
    CommonModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    SelectModule, TooltipModule, TextareaModule
  ]
})
export class ApprovalsModule {}
