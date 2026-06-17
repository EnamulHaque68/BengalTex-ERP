import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TooltipModule } from 'primeng/tooltip';

import { LayoutComponent } from './layout.component';
import { SidebarComponent } from './sidebar/sidebar.component';

@NgModule({
  declarations: [LayoutComponent, SidebarComponent],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,        // routerLink / routerLinkActive / <router-outlet>
    TooltipModule        // pTooltip for collapsed-rail labels
  ],
  exports: [LayoutComponent]
})
export class LayoutModule {}
