import { ChangeDetectorRef, Component, Input, NgZone, OnChanges, SimpleChanges } from '@angular/core';
import { AttachmentService } from '../../services/attachment.service';
import { AttachmentDto } from '../../models/attachment.models';

/**
 * Reusable attachments widget. Drop into any document detail view:
 *   <app-attachments-panel entityType="PurchaseOrder" [entityId]="id"></app-attachments-panel>
 * Polymorphic — (entityType, entityId) addresses any entity in the system.
 */
@Component({
  selector: 'app-attachments-panel',
  standalone: false,
  templateUrl: './attachments-panel.component.html',
  styleUrl: './attachments-panel.component.scss'
})
export class AttachmentsPanelComponent implements OnChanges {
  @Input() entityType!: string;
  @Input() entityId: number | null = null;
  @Input() canManage = true;

  attachments: AttachmentDto[] = [];
  loading = false;
  uploading = false;
  error = '';
  deletingId: number | null = null;
  selectedFile: File | null = null;
  description = '';

  constructor(
    private service: AttachmentService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['entityId'] || changes['entityType']) this.load();
  }

  load(): void {
    if (!this.entityType || !this.entityId) { this.attachments = []; return; }
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();
    this.service.list(this.entityType, this.entityId).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.attachments = res.data;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files && input.files.length ? input.files[0] : null;
  }

  upload(fileInput: HTMLInputElement): void {
    if (!this.selectedFile || !this.entityId || this.uploading) return;
    this.uploading = true;
    this.error = '';
    this.cdr.detectChanges();
    this.service.upload(this.entityType, this.entityId, this.selectedFile, this.description?.trim() || null)
      .subscribe({
        next: (res) => this.zone.run(() => {
          this.uploading = false;
          if (res.success) {
            this.selectedFile = null;
            this.description = '';
            if (fileInput) fileInput.value = '';
            this.load();
          } else {
            this.error = res.message || 'Upload failed.';
          }
          this.cdr.detectChanges();
        }),
        error: (err) => this.zone.run(() => {
          this.uploading = false;
          this.error = err?.error?.message || 'Upload failed.';
          this.cdr.detectChanges();
        })
      });
  }

  download(att: AttachmentDto): void {
    this.service.download(att.id).subscribe({
      next: (blob) => this.zone.run(() => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = att.fileName;
        a.click();
        URL.revokeObjectURL(url);
      }),
      error: () => this.zone.run(() => { this.error = 'Download failed.'; this.cdr.detectChanges(); })
    });
  }

  remove(att: AttachmentDto): void {
    if (this.deletingId) return;
    this.deletingId = att.id;
    this.error = '';
    this.cdr.detectChanges();
    this.service.delete(att.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.deletingId = null;
        if (res.success) this.load();
        else this.error = res.message || 'Delete failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.deletingId = null;
        this.error = err?.error?.message || 'Delete failed.';
        this.cdr.detectChanges();
      })
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  iconFor(contentType: string): string {
    if (!contentType) return 'pi-file';
    if (contentType.startsWith('image/')) return 'pi-image';
    if (contentType === 'application/pdf') return 'pi-file-pdf';
    if (contentType.includes('word')) return 'pi-file-word';
    if (contentType.includes('sheet') || contentType.includes('excel') || contentType.includes('csv'))
      return 'pi-file-excel';
    return 'pi-file';
  }
}
