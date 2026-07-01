import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { GoodsReceiptService } from '../../../services/goods-receipt.service';
import { PurchaseOrderService } from '../../../services/purchase-order.service';
import { WarehouseService } from '../../../services/warehouse.service';
import { LetterOfCreditService } from '../../../services/letter-of-credit.service';
import { PagedQueryParameters } from '../../../models/user.models';
import { GRN_STATUSES, GoodsReceiptDto, GoodsReceiptListItemDto } from '../../../models/goods-receipt.models';
import { LetterOfCreditListItemDto } from '../../../models/letter-of-credit.models';
import { PurchaseOrderDto, PurchaseOrderListItemDto } from '../../../models/purchase-order.models';
import { WarehouseDto } from '../../../models/master-data.models';

interface ReceivablePoOption {
  id: number;
  code: string;
  supplierName: string;
  status: string;
  displayLabel: string;     // "{code} — {supplierName}"
}

@Component({
  selector: 'app-goods-receipt-list',
  standalone: false,
  templateUrl: './goods-receipt-list.component.html',
  styleUrl: './goods-receipt-list.component.scss'
})
export class GoodsReceiptListComponent implements OnInit {

  grns: GoodsReceiptListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterPoId: number | null = null;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Dropdown sources
  readonly statuses = GRN_STATUSES;
  receivablePos: ReceivablePoOption[] = [];   // POs eligible for receiving
  allPosForFilter: ReceivablePoOption[] = []; // all POs for the list filter
  warehouses: WarehouseDto[] = [];
  selectedPo: PurchaseOrderDto | null = null;  // detail of PO currently in the dialog

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingGrn: GoodsReceiptListItemDto | null = null;
  deleting = false;
  deleteError = '';

  // Row action (post) in-flight id
  rowActionId: number | null = null;

  // ── Area B: linked Letter of Credit ──
  lcSuggestion: LetterOfCreditListItemDto | null = null;             // auto-suggested LC for the PO (create/edit)
  lcLinked: { code: string; number: string | null; status: string } | null = null;  // linked LC shown in view mode

  constructor(
    private grnService: GoodsReceiptService,
    private poService: PurchaseOrderService,
    private warehouseService: WarehouseService,
    private lcService: LetterOfCreditService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadDropdowns();
    this.load();

    // Traceability deep-link: /goods-receipts?open=<id> opens that GRN's details.
    const open = Number(this.route.snapshot.queryParamMap.get('open'));
    if (open > 0) this.openEdit({ id: open } as GoodsReceiptListItemDto);
  }

  // ─── Form ────────────────────────────────────────────────────────────────

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private buildForm(): void {
    this.form = this.fb.group({
      purchaseOrderId: [null as number | null, Validators.required],
      letterOfCreditId: [null as number | null],
      receiveDate: [this.todayIso(), Validators.required],
      receivingWarehouseId: [null as number | null, Validators.required],
      supplierDeliveryRef: ['', Validators.maxLength(100)],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  // ─── Data loading ────────────────────────────────────────────────────────

  private loadDropdowns(): void {
    this.poService.getAll({ page: 1, pageSize: 500, search: '' }).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const mapped: ReceivablePoOption[] = res.data.items.map(p => ({
              id: p.id,
              code: p.code,
              supplierName: p.supplierName,
              status: p.status,
              displayLabel: `${p.code} — ${p.supplierName}`
            }));
            this.allPosForFilter = mapped;
            this.receivablePos = mapped.filter(p =>
              p.status === 'Approved' || p.status === 'Sent' || p.status === 'PartiallyReceived');
          }
          this.cdr.detectChanges();
        });
      }
    });
    this.warehouseService.getAll(undefined, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success) this.warehouses = res.data ?? [];
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.grnService.getAll(
      this.parameters,
      this.filterPoId ?? undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.grns = res.data.items;
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

  // ─── PO-driven line building ─────────────────────────────────────────────

  /** Build/replace the lines FormArray from a PO's lines. If editing, prefill from existingGrn. */
  private buildLinesFromPo(po: PurchaseOrderDto, existingGrn?: GoodsReceiptDto): void {
    this.lines.clear();
    for (const poLine of po.lines) {
      const remaining = poLine.quantity - poLine.receivedQuantity;
      const existing = existingGrn?.lines.find(l => l.purchaseOrderLineId === poLine.id);

      // Skip lines that have nothing left and don't already appear on this GRN
      if (remaining <= 0 && !existing) continue;

      this.lines.push(this.fb.group({
        purchaseOrderLineId: [poLine.id, Validators.required],
        rawMaterialDisplay: [`${poLine.rawMaterialCode} — ${poLine.rawMaterialName}`],
        uomCode: [poLine.unitOfMeasureCode],
        orderedQuantity: [poLine.quantity],
        alreadyReceived: [poLine.receivedQuantity],
        remaining: [remaining],
        receivedQuantity: [existing?.receivedQuantity ?? remaining, [Validators.required, Validators.min(0)]],
        lineNotes: [existing?.lineNotes ?? '', Validators.maxLength(1000)],
        lotNumber: [existing?.lotNumber ?? '', Validators.maxLength(100)],
        shade: [existing?.shade ?? '', Validators.maxLength(100)],
        manufactureDate: [existing?.manufactureDate ?? null],
        expiryDate: [existing?.expiryDate ?? null]
      }));
    }
  }

  onPoChange(event: any): void {
    const poId = event?.value;
    this.lcSuggestion = null;
    this.form.patchValue({ letterOfCreditId: null });
    if (!poId) {
      this.lines.clear();
      this.selectedPo = null;
      return;
    }
    this.fetchPoAndBuildLines(poId, undefined);
    this.loadLcForPo(poId);
  }

  private fetchPoAndBuildLines(poId: number, existingGrn?: GoodsReceiptDto): void {
    this.poService.getById(poId).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.selectedPo = res.data;
            this.buildLinesFromPo(res.data, existingGrn);
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  /** Auto-suggest the PO's letter of credit (import purchases). Null for local / non-LC POs. */
  private loadLcForPo(poId: number): void {
    this.lcService.getForPurchaseOrder(poId).subscribe({
      next: (res) => this.zone.run(() => {
        this.lcSuggestion = res.success ? (res.data ?? null) : null;
        this.form.patchValue({ letterOfCreditId: this.lcSuggestion?.id ?? null });
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.lcSuggestion = null; this.cdr.detectChanges(); })
    });
  }

  // ─── Create / Edit / View dialog ─────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.selectedPo = null;
    this.lcSuggestion = null;
    this.lcLinked = null;
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      purchaseOrderId: this.filterPoId ?? null,
      letterOfCreditId: null,
      receiveDate: this.todayIso(),
      receivingWarehouseId: this.warehouses[0]?.id ?? null,
      supplierDeliveryRef: '',
      notes: ''
    });
    this.dialogVisible = true;
    // If filter pre-fills a PO, load its lines + LC suggestion
    if (this.form.get('purchaseOrderId')?.value) {
      this.fetchPoAndBuildLines(this.form.get('purchaseOrderId')!.value, undefined);
      this.loadLcForPo(this.form.get('purchaseOrderId')!.value);
    }
  }

  openEdit(grn: GoodsReceiptListItemDto): void {
    this.editingId = grn.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.selectedPo = null;
    this.lcSuggestion = null;
    this.lcLinked = null;
    this.dialogVisible = true;

    this.grnService.getById(grn.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const g = res.data;
            this.dialogMode = g.status === 'Draft' ? 'edit' : 'view';
            this.lcLinked = g.letterOfCreditId
              ? { code: g.letterOfCreditCode!, number: g.letterOfCreditNumber, status: g.letterOfCreditStatus! }
              : null;
            this.form.patchValue({
              purchaseOrderId: g.purchaseOrderId,
              letterOfCreditId: g.letterOfCreditId,
              receiveDate: g.receiveDate,
              receivingWarehouseId: g.receivingWarehouseId,
              supplierDeliveryRef: g.supplierDeliveryRef ?? '',
              notes: g.notes ?? ''
            });

            if (this.dialogMode === 'edit') {
              // Fetch PO to compute remaining; prefill lines with existing GRN qty
              this.fetchPoAndBuildLines(g.purchaseOrderId, g);
            } else {
              // View mode for Posted GRN — show its own lines as-is, no PO fetch needed
              this.buildLinesFromGrnReadonly(g);
              this.form.disable();
            }
            // PO is fixed once the GRN exists
            this.form.get('purchaseOrderId')?.disable();
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  private buildLinesFromGrnReadonly(grn: GoodsReceiptDto): void {
    this.lines.clear();
    for (const l of grn.lines) {
      this.lines.push(this.fb.group({
        purchaseOrderLineId: [l.purchaseOrderLineId],
        rawMaterialDisplay: [`${l.rawMaterialCode} — ${l.rawMaterialName}`],
        uomCode: [l.unitOfMeasureCode],
        orderedQuantity: [l.orderedQuantity],
        alreadyReceived: [0],
        remaining: [0],
        receivedQuantity: [l.receivedQuantity],
        lineNotes: [l.lineNotes ?? ''],
        lotNumber: [l.lotNumber ?? ''],
        shade: [l.shade ?? ''],
        manufactureDate: [l.manufactureDate ?? null],
        expiryDate: [l.expiryDate ?? null]
      }));
    }
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving || this.dialogMode === 'view') return;

    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();
    const lines = (v.lines as any[])
      .filter(l => Number(l.receivedQuantity) > 0)
      .map(l => ({
        purchaseOrderLineId: l.purchaseOrderLineId,
        receivedQuantity: Number(l.receivedQuantity),
        lineNotes: (l.lineNotes as string)?.trim() || null,
        lotNumber: (l.lotNumber as string)?.trim() || null,
        shade: (l.shade as string)?.trim() || null,
        manufactureDate: (l.manufactureDate as string) || null,
        expiryDate: (l.expiryDate as string) || null
      }));

    if (lines.length === 0) {
      this.dialogSaving = false;
      this.dialogError = 'Enter at least one received quantity (> 0).';
      this.cdr.detectChanges();
      return;
    }

    const baseFields = {
      receiveDate: v.receiveDate,
      receivingWarehouseId: v.receivingWarehouseId,
      supplierDeliveryRef: (v.supplierDeliveryRef as string)?.trim() || null,
      notes: (v.notes as string)?.trim() || null,
      lines
    };

    if (this.dialogMode === 'create') {
      this.grnService.create({
        purchaseOrderId: v.purchaseOrderId,
        letterOfCreditId: v.letterOfCreditId ?? null,
        ...baseFields
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.grnService.update(this.editingId, baseFields).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    }
  }

  private handleSave(res: { success: boolean; message?: string | null }): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      if (res.success) {
        this.dialogVisible = false;
        this.load();
      } else {
        this.dialogError = res.message || 'Save failed.';
      }
      this.cdr.detectChanges();
    });
  }

  private handleError(err: any): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      this.dialogError = err?.error?.message || 'Save failed.';
      this.cdr.detectChanges();
    });
  }

  // ─── Row actions: post ───────────────────────────────────────────────────

  post(grn: GoodsReceiptListItemDto): void {
    if (this.rowActionId) return;
    this.rowActionId = grn.id;
    this.actionError = '';
    this.cdr.detectChanges();
    this.grnService.post(grn.id).subscribe({
      next: (res) => this.handleRowAction(res),
      error: (err) => this.handleRowActionError(err)
    });
  }

  private handleRowAction(res: { success: boolean; message?: string | null }): void {
    this.zone.run(() => {
      this.rowActionId = null;
      if (res.success) {
        this.actionError = '';
        this.load();
      } else {
        this.actionError = res.message || 'Action failed.';
      }
      this.cdr.detectChanges();
    });
  }

  private handleRowActionError(err: any): void {
    this.zone.run(() => {
      this.rowActionId = null;
      this.actionError = err?.error?.message || 'Action failed.';
      this.cdr.detectChanges();
    });
  }

  // ─── Delete ──────────────────────────────────────────────────────────────

  confirmDelete(grn: GoodsReceiptListItemDto): void {
    this.deletingGrn = grn;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingGrn || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.grnService.delete(this.deletingGrn.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingGrn = null;
            this.load();
          } else {
            this.deleteError = res.message || 'Delete failed.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.deleting = false;
          this.deleteError = err?.error?.message || 'Delete failed.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  // ─── Display helpers ─────────────────────────────────────────────────────

  meta(line: AbstractControl) {
    return line.getRawValue();
  }

  formatDate(d: string | null): string {
    return d ? d : '—';
  }
}
