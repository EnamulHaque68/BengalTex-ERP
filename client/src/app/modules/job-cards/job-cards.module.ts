import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { TooltipModule } from 'primeng/tooltip';
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';

import { MachineListComponent } from './machine-list/machine-list.component';
import { JobCardListComponent } from './job-card-list/job-card-list.component';
import { JobCardDetailComponent } from './job-card-detail/job-card-detail.component';
import { JobCardScannerComponent } from './job-card-scanner/job-card-scanner.component';

const routes: Routes = [
  { path: '', component: JobCardListComponent },
  { path: 'machines', component: MachineListComponent },
  { path: 'scan', component: JobCardScannerComponent },
  { path: ':id', component: JobCardDetailComponent }
];

@NgModule({
  declarations: [MachineListComponent, JobCardListComponent, JobCardDetailComponent, JobCardScannerComponent],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, SelectModule, CheckboxModule, TooltipModule,
    TextareaModule, TagModule, ToggleSwitchModule
  ]
})
export class JobCardsModule {}
