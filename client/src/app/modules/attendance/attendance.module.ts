import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';

import { SharedModule } from '../../shared/shared.module';
import { AttendanceListComponent } from './attendance-list/attendance-list.component';
import { SelfCheckInComponent } from './self-check-in/self-check-in.component';
import { MyAttendanceComponent } from './my-attendance/my-attendance.component';
import { TeamAttendanceComponent } from './team-attendance/team-attendance.component';
import { AttendanceSettingsComponent } from './attendance-settings/attendance-settings.component';
import { OfficeLocationsComponent } from './office-locations/office-locations.component';
import { AttendanceReportsComponent } from './attendance-reports/attendance-reports.component';

const routes: Routes = [
  { path: '', component: AttendanceListComponent },
  { path: 'my', component: MyAttendanceComponent },
  { path: 'team', component: TeamAttendanceComponent },
  { path: 'settings', component: AttendanceSettingsComponent },
  { path: 'office-locations', component: OfficeLocationsComponent },
  { path: 'reports', component: AttendanceReportsComponent },
  { path: 'check-in', component: SelfCheckInComponent }
];

@NgModule({
  declarations: [
    AttendanceListComponent, SelfCheckInComponent, MyAttendanceComponent, TeamAttendanceComponent,
    AttendanceSettingsComponent, OfficeLocationsComponent, AttendanceReportsComponent
  ],
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule,
    RouterModule.forChild(routes),
    CardModule, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, TextareaModule, SelectModule, TooltipModule,
    SharedModule
  ]
})
export class AttendanceModule {}
