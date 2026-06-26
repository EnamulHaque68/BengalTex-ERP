import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ToggleButtonModule } from 'primeng/togglebutton';
import { CheckboxModule } from 'primeng/checkbox';

import { MrpListComponent } from './mrp-list/mrp-list.component';

const routes: Routes = [{ path: '', component: MrpListComponent }];

@NgModule({
  declarations: [MrpListComponent],
  imports: [
    CommonModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, ToggleButtonModule, CheckboxModule
  ]
})
export class MrpModule {}
