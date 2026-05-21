import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';

import { AttachmentsPanelComponent } from './attachments-panel/attachments-panel.component';

/**
 * Shared, cross-feature UI building blocks. Import into any lazy feature module
 * that needs a reusable widget (e.g. the attachments panel).
 */
@NgModule({
  declarations: [AttachmentsPanelComponent],
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, TooltipModule],
  exports: [AttachmentsPanelComponent]
})
export class SharedModule {}
