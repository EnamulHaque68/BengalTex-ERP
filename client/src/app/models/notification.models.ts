// ─── Notifications ──────────────────────────────────────────────────────────

export const NOTIFICATION_CHANNELS: { label: string; value: string }[] = [
  { label: 'In-App', value: 'InApp' },
  { label: 'Email', value: 'Email' },
  { label: 'SMS', value: 'Sms' }
];

export const NOTIFICATION_STATUSES: { label: string; value: string }[] = [
  { label: 'Sent', value: 'Sent' },
  { label: 'Failed', value: 'Failed' },
  { label: 'Pending', value: 'Pending' }
];

export interface NotificationDto {
  id: number;
  channel: string;                    // InApp | Email | Sms
  recipient: string;
  subject: string;
  body: string;
  relatedEntityType: string | null;
  relatedEntityId: number | null;
  status: string;                     // Sent | Failed | Pending
  error: string | null;
  sentAt: string | null;
  createdAt: string;
}

export interface SendTestNotificationRequest {
  channel: string;
  recipient: string;
  subject: string;
  body: string;
}
