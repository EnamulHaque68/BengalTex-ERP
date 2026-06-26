import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MrpService } from '../../../services/mrp.service';
import { MrpItemDto } from '../../../models/mrp.models';

@Component({
  selector: 'app-mrp-list',
  standalone: false,
  templateUrl: './mrp-list.component.html',
  styleUrl: './mrp-list.component.scss'
})
export class MrpListComponent implements OnInit {

  items: MrpItemDto[] = [];
  loading = false;
  shortageOnly = false;
  shortageCount = 0;
  totalShortageCost = 0;

  // selected raw-material ids for PR generation (shortage rows only)
  selected = new Set<number>();
  generating = false;
  actionError = '';
  actionMessage = '';

  constructor(
    private mrpService: MrpService,
    private router: Router,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.actionError = '';
    this.actionMessage = '';
    this.mrpService.getMrp(this.shortageOnly).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.items = res.data.items;
            this.shortageCount = res.data.shortageCount;
            this.totalShortageCost = res.data.totalEstimatedShortageCost;
            // drop selections no longer present
            const present = new Set(this.items.map(i => i.rawMaterialId));
            this.selected.forEach(id => { if (!present.has(id)) this.selected.delete(id); });
          }
          this.cdr.detectChanges();
        });
      },
      error: () => this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); })
    });
  }

  hasShortage(i: MrpItemDto): boolean {
    return i.shortageQuantity > 0;
  }

  toggle(i: MrpItemDto): void {
    if (!this.hasShortage(i)) return;
    if (this.selected.has(i.rawMaterialId)) this.selected.delete(i.rawMaterialId);
    else this.selected.add(i.rawMaterialId);
  }

  isSelected(i: MrpItemDto): boolean {
    return this.selected.has(i.rawMaterialId);
  }

  selectAllShortages(): void {
    this.items.filter(i => this.hasShortage(i)).forEach(i => this.selected.add(i.rawMaterialId));
  }

  clearSelection(): void {
    this.selected.clear();
  }

  generatePr(): void {
    if (this.generating || this.selected.size === 0) return;
    this.generating = true;
    this.actionError = '';
    this.actionMessage = '';
    this.cdr.detectChanges();

    this.mrpService.generateRequisition(Array.from(this.selected)).subscribe({
      next: (res) => this.zone.run(() => {
        this.generating = false;
        if (res.success) {
          this.actionMessage = res.message || 'Purchase requisition created.';
          this.selected.clear();
          this.router.navigate(['/purchase-requisitions']);
        } else {
          this.actionError = res.message || 'Could not generate requisition.';
        }
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.generating = false;
        this.actionError = err?.error?.message || 'Could not generate requisition.';
        this.cdr.detectChanges();
      })
    });
  }
}
