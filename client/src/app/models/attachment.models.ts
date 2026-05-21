// ─── Document Attachments ───────────────────────────────────────────────────

export interface AttachmentDto {
  id: number;
  entityType: string;
  entityId: number;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  description: string | null;
  category: string | null;
  uploadedAt: string;
  uploadedBy: string | null;
}
