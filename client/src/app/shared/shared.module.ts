import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';

import { AttachmentsPanelComponent } from './attachments-panel/attachments-panel.component';
import { SendEmailDialogComponent } from './send-email-dialog/send-email-dialog.component';
import { TranslatePipe } from './i18n/translate.pipe';
import { LangToggleComponent } from './i18n/lang-toggle.component';

/**
 * Shared, cross-feature UI building blocks. Import into any lazy feature module
 * that needs a reusable widget (e.g. the attachments panel or send-email dialog)
 * or the `t` translation pipe for operator-facing Bangla screens.
 */
@NgModule({
  declarations: [AttachmentsPanelComponent, SendEmailDialogComponent, TranslatePipe, LangToggleComponent],
  imports: [CommonModule, FormsModule, ButtonModule, DialogModule, InputTextModule, TextareaModule, TooltipModule],
  exports: [AttachmentsPanelComponent, SendEmailDialogComponent, TranslatePipe, LangToggleComponent]
})
export class SharedModule {}
