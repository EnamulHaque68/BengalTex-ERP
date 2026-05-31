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

import { SharedModule } from '../../shared/shared.module';

import { ComplianceDashboardComponent } from './compliance-dashboard/compliance-dashboard.component';
import { CertificateListComponent } from './certificate-list/certificate-list.component';
import { AuditListComponent } from './audit-list/audit-list.component';

const routes: Routes = [
  { path: '', component: ComplianceDashboardComponent },
  { path: 'certificates', component: CertificateListComponent },
  { path: 'audits', component: AuditListComponent }
];

@NgModule({
  declarations: [ComplianceDashboardComponent, CertificateListComponent, AuditListComponent],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, SelectModule, CheckboxModule, TooltipModule,
    TextareaModule, TagModule, ToggleSwitchModule,
    SharedModule
  ]
})
export class ComplianceModule {}
