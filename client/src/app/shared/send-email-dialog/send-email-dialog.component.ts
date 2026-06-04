import {
  ChangeDetectorRef, Component, EventEmitter, Input, NgZone, OnChanges, Output, SimpleChanges
} from '@angular/core';
import { EmailService } from '../../services/email.service';

/**
 * Reusable Send Email dialog. Drop into any feature module that imports SharedModule
 * and bind:
 *   [visible]="emailDlgOpen" (visibleChange)="emailDlgOpen=$event"
 *   [sourceType]="'CustomerInvoice'" [sourceId]="row.id"
 *   (sent)="onEmailSent($event)"
 *
 * On open it calls /api/emails/preview to pre-render the body + default recipient,
 * then user edits To / Cc / Subject / Body (HTML) and clicks Send.
 */
@Component({
  selector: 'app-send-email-dialog',
  standalone: false,
  templateUrl: './send-email-dialog.component.html',
  styleUrls: ['./send-email-dialog.component.scss']
})
export class SendEmailDialogComponent implements OnChanges {
  @Input() visible = false;
  @Input() sourceType = '';
  @Input() sourceId = 0;

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() sent = new EventEmitter<{ sourceType: string; sourceId: number; sourceCode: string }>();

  loading = false;
  sending = false;
  error = '';
  sourceCode = '';
  to = '';
  cc = '';
  subject = '';
  htmlBody = '';
  showRawHtml = false;

  constructor(
    private svc: EmailService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible && this.sourceType && this.sourceId) {
      this.loadPreview();
    }
  }

  close(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  loadPreview(): void {
    this.loading = true;
    this.error = '';
    this.to = ''; this.cc = ''; this.subject = ''; this.htmlBody = ''; this.sourceCode = '';
    this.cdr.detectChanges();
    this.svc.preview(this.sourceType, this.sourceId).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) {
          this.sourceCode = res.data.sourceCode;
          this.subject = res.data.defaultSubject;
          this.htmlBody = res.data.htmlBody;
          this.to = res.data.defaultToAddress ?? '';
        } else {
          this.error = res.message || 'Failed to load document.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loading = false;
        this.error = err?.error?.message || 'Failed to load document.';
        this.cdr.detectChanges();
      })
    });
  }

  send(): void {
    if (!this.to?.trim() || !this.subject?.trim() || !this.htmlBody?.trim() || this.sending) return;
    this.sending = true;
    this.error = '';
    this.cdr.detectChanges();
    this.svc.send({
      sourceType: this.sourceType,
      sourceId: this.sourceId,
      toAddresses: this.to.trim(),
      ccAddresses: this.cc?.trim() || null,
      subject: this.subject.trim(),
      htmlBody: this.htmlBody
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.sending = false;
        if (res.success) {
          this.sent.emit({ sourceType: this.sourceType, sourceId: this.sourceId, sourceCode: this.sourceCode });
          this.close();
        } else {
          this.error = res.message || 'Send failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.sending = false;
        this.error = err?.error?.message || 'Send failed.';
        this.cdr.detectChanges();
      })
    });
  }
}
