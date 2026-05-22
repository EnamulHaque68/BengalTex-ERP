import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { NotificationService } from '../../../services/notification.service';
import { AuthService } from '../../../services/auth.service';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  NOTIFICATION_CHANNELS, NOTIFICATION_STATUSES,
  NotificationDto, SendTestNotificationRequest
} from '../../../models/notification.models';

@Component({
  selector: 'app-notification-list',
  standalone: false,
  templateUrl: './notification-list.component.html',
  styleUrl: './notification-list.component.scss'
})
export class NotificationListComponent implements OnInit {

  notifications: NotificationDto[] = [];
  loading = false;
  totalCount = 0;

  filterChannel: string | null = null;
  filterStatus: string | null = null;

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly channels = NOTIFICATION_CHANNELS;
  readonly statuses = NOTIFICATION_STATUSES;

  canSend = false;

  // View dialog
  viewDialogVisible = false;
  viewing: NotificationDto | null = null;

  // Send-test dialog
  sendDialogVisible = false;
  sending = false;
  sendError = '';
  sendSuccess = '';
  testModel: SendTestNotificationRequest = this.blankTest();

  constructor(
    private notificationService: NotificationService,
    private auth: AuthService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.canSend = this.auth.hasPermission('Notifications.Send');
    this.load();
  }

  load(): void {
    this.loading = true;
    this.notificationService.getAll(
      this.parameters,
      this.filterChannel ?? undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.notifications = res.data.items;
            this.totalCount = res.data.totalCount;
          }
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); });
      }
    });
  }

  onSearchChange(value: string): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.parameters.search = value;
      this.parameters.page = 1;
      this.load();
    }, 400);
  }

  onPageChange(event: any): void {
    this.parameters.page = Math.floor(event.first / event.rows) + 1;
    this.parameters.pageSize = event.rows;
    this.load();
  }

  openView(n: NotificationDto): void {
    this.viewing = n;
    this.viewDialogVisible = true;
  }

  // ─── Send test ──────────────────────────────────────────────────────────
  openSend(): void {
    this.testModel = this.blankTest();
    this.sendError = '';
    this.sendSuccess = '';
    this.sendDialogVisible = true;
  }

  sendTest(): void {
    this.sending = true;
    this.sendError = '';
    this.sendSuccess = '';
    this.notificationService.sendTest(this.testModel).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.sending = false;
          if (res.success) {
            this.sendSuccess = res.message || 'Notification dispatched.';
            this.load();   // refresh the log to show the new attempt
          } else {
            this.sendError = res.message || 'Send failed.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.sending = false;
          this.sendError = err?.error?.message || 'Send failed.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  private blankTest(): SendTestNotificationRequest {
    return { channel: 'InApp', recipient: '', subject: '', body: '' };
  }

  channelClass(channel: string): string {
    switch (channel) {
      case 'Email': return 'email';
      case 'Sms':   return 'sms';
      case 'InApp': return 'inapp';
      default:      return '';
    }
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Sent':    return 'sent';
      case 'Failed':  return 'failed';
      case 'Pending': return 'pending';
      default:        return '';
    }
  }

  channelLabel(channel: string): string {
    return this.channels.find(c => c.value === channel)?.label ?? channel;
  }
}
