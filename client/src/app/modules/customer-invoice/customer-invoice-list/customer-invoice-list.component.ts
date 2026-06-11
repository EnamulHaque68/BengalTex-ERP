import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CustomerInvoiceService } from '../../../services/customer-invoice.service';
import { SalesOrderService } from '../../../services/sales-order.service';
import { CustomerService } from '../../../services/customer.service';
import { MasterSetupService } from '../../../services/master-setup.service';
import { BankAccountDto } from '../../../models/master-setup.models';
import { PagedQueryParameters } from '../../../models/user.models';
import {
  CINV_STATUSES,
  CustomerInvoiceDto,
  CustomerInvoiceListItemDto
} from '../../../models/customer-invoice.models';
import { SalesOrderDto, SalesOrderListItemDto } from '../../../models/sales-order.models';
import { CustomerListItemDto } from '../../../models/customer.models';

interface InvoiceableSoOption {
  id: number;
  code: string;
  customerName: string;
  status: string;
  displayLabel: string;
}

@Component({
  selector: 'app-customer-invoice-list',
  standalone: false,
  templateUrl: './customer-invoice-list.component.html',
  styleUrl: './customer-invoice-list.component.scss'
})
export class CustomerInvoiceListComponent implements OnInit {

  invoices: CustomerInvoiceListItemDto[] = [];
  loading = false;
  totalCount = 0;
  filterCustomerId: number | null = null;
  filterStatus: string | null = null;
  actionError = '';

  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  readonly statuses = CINV_STATUSES;
  customers: CustomerListItemDto[] = [];
  invoiceableSos: InvoiceableSoOption[] = [];
  selectedSo: SalesOrderDto | null = null;

  dialogVisible = false;
  dialogMode: 'create' | 'edit' | 'view' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;

  // Email dialog
  emailDlgOpen = false;
  emailSourceId = 0;
  actionMessage = '';

  openEmail(row: { id: number }): void {
    this.emailSourceId = row.id;
    this.emailDlgOpen = true;
  }

  onEmailSent(ev: { sourceCode: string }): void {
    this.actionMessage = `Email sent for ${ev.sourceCode}.`;
    this.cdr.detectChanges();
  }

  // Export details (EPB Form-N + Commercial Invoice + Packing List) dialog
  exportDlgVisible = false;
  exportSaving = false;
  exportError = '';
  exportTarget: CustomerInvoiceListItemDto | null = null;
  exportForm = {
    epbFormNumber: '', lcNumber: '', shipmentDate: '',
    incoTerm: '', portOfLoading: '', portOfDischarge: '',
    vesselName: '', countryOfDestination: '', shippingMarks: '',
    totalPackages: null as number | null,
    grossWeightKg: null as number | null,
    netWeightKg: null as number | null,
    containerNumber: '', sealNumber: '', truckNumber: '',
    beneficiaryBankAccountId: null as number | null
  };

  openMarkExported(row: CustomerInvoiceListItemDto): void {
    this.exportTarget = row;
    this.exportError = '';
    // Defaults; full record fetched below for editing prior shipping data.
    this.exportForm = {
      epbFormNumber: row.epbFormNumber ?? '',
      lcNumber: '',
      shipmentDate: row.shipmentDate ?? '',
      incoTerm: '', portOfLoading: 'Chattogram, Bangladesh', portOfDischarge: '',
      vesselName: '', countryOfDestination: '', shippingMarks: '',
      totalPackages: null, grossWeightKg: null, netWeightKg: null,
      containerNumber: '', sealNumber: '', truckNumber: '',
      beneficiaryBankAccountId: null
    };
    this.exportDlgVisible = true;
    if (this.bankAccounts.length === 0) {
      this.masterSetup.getBankAccounts(false).subscribe({
        next: (res) => this.zone.run(() => {
          if (res.success && res.data) this.bankAccounts = res.data;
          this.cdr.detectChanges();
        })
      });
    }
    this.invService.getById(row.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          const d = res.data;
          this.exportForm = {
            epbFormNumber: d.epbFormNumber ?? '',
            lcNumber: d.lcNumber ?? '',
            shipmentDate: d.shipmentDate ?? '',
            incoTerm: d.incoTerm ?? '',
            portOfLoading: d.portOfLoading ?? 'Chattogram, Bangladesh',
            portOfDischarge: d.portOfDischarge ?? '',
            vesselName: d.vesselName ?? '',
            countryOfDestination: d.countryOfDestination ?? '',
            shippingMarks: d.shippingMarks ?? '',
            totalPackages: d.totalPackages,
            grossWeightKg: d.grossWeightKg,
            netWeightKg: d.netWeightKg,
            containerNumber: d.containerNumber ?? '',
            sealNumber: d.sealNumber ?? '',
            truckNumber: d.truckNumber ?? '',
            beneficiaryBankAccountId: d.beneficiaryBankAccountId ?? null
          };
          this.cdr.detectChanges();
        }
      })
    });
  }

  saveExport(): void {
    if (!this.exportTarget || this.exportSaving) return;
    this.exportSaving = true;
    this.exportError = '';
    this.cdr.detectChanges();
    const f = this.exportForm;
    this.invService.markExported(this.exportTarget.id, {
      epbFormNumber: f.epbFormNumber?.trim() || null,
      lcNumber: f.lcNumber?.trim() || null,
      shipmentDate: f.shipmentDate || null,
      incoTerm: f.incoTerm?.trim() || null,
      portOfLoading: f.portOfLoading?.trim() || null,
      portOfDischarge: f.portOfDischarge?.trim() || null,
      vesselName: f.vesselName?.trim() || null,
      countryOfDestination: f.countryOfDestination?.trim() || null,
      shippingMarks: f.shippingMarks?.trim() || null,
      totalPackages: f.totalPackages ?? null,
      grossWeightKg: f.grossWeightKg ?? null,
      netWeightKg: f.netWeightKg ?? null,
      containerNumber: f.containerNumber?.trim() || null,
      sealNumber: f.sealNumber?.trim() || null,
      truckNumber: f.truckNumber?.trim() || null,
      beneficiaryBankAccountId: f.beneficiaryBankAccountId ?? null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.exportSaving = false;
        if (res.success) {
          this.exportDlgVisible = false;
          this.actionMessage = `Export details saved for ${this.exportTarget?.code}.`;
          this.load();
        } else {
          this.exportError = res.message || 'Save failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => {
        this.exportSaving = false;
        this.exportError = e?.error?.message || 'Save failed.';
        this.cdr.detectChanges();
      })
    });
  }

  openCommercialInvoice(row: CustomerInvoiceListItemDto): void {
    window.open(`/customer-invoices/${row.id}/print-commercial`, '_blank');
  }

  openPackingList(row: CustomerInvoiceListItemDto): void {
    window.open(`/customer-invoices/${row.id}/print-packing`, '_blank');
  }

  // ─── Line packing breakdown dialog ──────────────────────────────────────
  packingDlgVisible = false;
  packingSaving = false;
  packingError = '';
  packingTarget: CustomerInvoiceListItemDto | null = null;
  packingLines: Array<{
    lineId: number; productCode: string; productName: string; uom: string; quantity: number;
    cartonNumberFrom: number | null; cartonNumberTo: number | null;
    unitsPerCarton: number | null;
    netWeightKgPerLine: number | null; grossWeightKgPerLine: number | null;
  }> = [];

  openLinePacking(row: CustomerInvoiceListItemDto): void {
    this.packingTarget = row;
    this.packingError = '';
    this.packingLines = [];
    this.packingDlgVisible = true;
    this.invService.getById(row.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) {
          this.packingLines = res.data.lines.map(l => ({
            lineId: l.id,
            productCode: l.productCode,
            productName: l.productName,
            uom: l.unitOfMeasureCode,
            quantity: l.quantity,
            cartonNumberFrom: l.cartonNumberFrom,
            cartonNumberTo: l.cartonNumberTo,
            unitsPerCarton: l.unitsPerCarton,
            netWeightKgPerLine: l.netWeightKgPerLine,
            grossWeightKgPerLine: l.grossWeightKgPerLine
          }));
          this.cdr.detectChanges();
        }
      })
    });
  }

  saveLinePacking(): void {
    if (!this.packingTarget || this.packingSaving) return;
    this.packingSaving = true;
    this.packingError = '';
    this.cdr.detectChanges();
    const payload = {
      lines: this.packingLines.map(l => ({
        lineId: l.lineId,
        cartonNumberFrom: l.cartonNumberFrom ?? null,
        cartonNumberTo: l.cartonNumberTo ?? null,
        unitsPerCarton: l.unitsPerCarton ?? null,
        netWeightKgPerLine: l.netWeightKgPerLine ?? null,
        grossWeightKgPerLine: l.grossWeightKgPerLine ?? null
      }))
    };
    this.invService.setLinesPacking(this.packingTarget.id, payload).subscribe({
      next: (res) => this.zone.run(() => {
        this.packingSaving = false;
        if (res.success) {
          this.packingDlgVisible = false;
          this.actionMessage = `Line packing saved for ${this.packingTarget?.code}.`;
        } else {
          this.packingError = res.message || 'Save failed.';
        }
        this.cdr.detectChanges();
      }),
      error: (e) => this.zone.run(() => {
        this.packingSaving = false;
        this.packingError = e?.error?.message || 'Save failed.';
        this.cdr.detectChanges();
      })
    });
  }

  form!: FormGroup;

  deleteDialogVisible = false;
  deletingInv: CustomerInvoiceListItemDto | null = null;
  deleting = false;
  deleteError = '';

  rowActionId: number | null = null;

  bankAccounts: BankAccountDto[] = [];

  constructor(
    private invService: CustomerInvoiceService,
    private soService: SalesOrderService,
    private customerService: CustomerService,
    private masterSetup: MasterSetupService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadDropdowns();
    this.load();
  }

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private buildForm(): void {
    this.form = this.fb.group({
      salesOrderId: [null as number | null, Validators.required],
      // Bangladesh standard 15% VAT — stored as decimal fraction (0.15). User edits as percent.
      vatRatePercent: [15, [Validators.required, Validators.min(0), Validators.max(100)]],
      invoiceDate: [this.todayIso(), Validators.required],
      dueDate: [null as string | null],
      notes: ['', Validators.maxLength(2000)],
      lines: this.fb.array([])
    });
  }

  get vatRateDecimal(): number {
    return (Number(this.form.get('vatRatePercent')?.value) || 0) / 100;
  }

  subtotalAmount(): number {
    return this.lines.controls.reduce((sum, l) => sum + this.lineTotal(l), 0);
  }

  vatAmount(): number {
    return Math.round(this.subtotalAmount() * this.vatRateDecimal * 10000) / 10000;
  }

  grandTotalAmount(): number {
    return this.subtotalAmount() + this.vatAmount();
  }

  get lines(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  private loadDropdowns(): void {
    this.customerService.getAll({ page: 1, pageSize: 500, search: '' }, false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) this.customers = res.data.items;
          this.cdr.detectChanges();
        });
      }
    });
    this.soService.getAll({ page: 1, pageSize: 500, search: '' }).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.invoiceableSos = res.data.items
              .filter(s =>
                s.status === 'Confirmed' || s.status === 'PartiallyDispatched' ||
                s.status === 'Dispatched' || s.status === 'Delivered')
              .map(s => ({
                id: s.id,
                code: s.code,
                customerName: s.customerName,
                status: s.status,
                displayLabel: `${s.code} — ${s.customerName}`
              }));
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.invService.getAll(
      this.parameters,
      this.filterCustomerId ?? undefined,
      undefined,
      this.filterStatus ?? undefined
    ).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.invoices = res.data.items;
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

  private newLine(productId: number | null, productDisplay: string, uomCode: string,
                  quantity: number, unitPrice: number, lineNotes = ''): FormGroup {
    return this.fb.group({
      productId: [productId, Validators.required],
      productDisplay: [productDisplay],
      uomCode: [uomCode],
      quantity: [quantity, [Validators.required, Validators.min(0.0001)]],
      unitPrice: [unitPrice, [Validators.required, Validators.min(0)]],
      lineNotes: [lineNotes, Validators.maxLength(1000)]
    });
  }

  private buildLinesFromSo(so: SalesOrderDto): void {
    this.lines.clear();
    for (const soLine of so.lines) {
      this.lines.push(this.newLine(
        soLine.productId,
        `${soLine.productCode} — ${soLine.productName}`,
        soLine.unitOfMeasureCode,
        soLine.quantity,
        soLine.unitPrice,
        ''
      ));
    }
  }

  onSoChange(event: any): void {
    const soId = event?.value;
    if (!soId) {
      this.lines.clear();
      this.selectedSo = null;
      return;
    }
    this.soService.getById(soId).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.selectedSo = res.data;
            this.buildLinesFromSo(res.data);
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.selectedSo = null;
    this.form.enable();
    this.lines.clear();
    this.form.reset({
      salesOrderId: null,
      vatRatePercent: 15,
      invoiceDate: this.todayIso(),
      dueDate: null,
      notes: ''
    });
    this.dialogVisible = true;
  }

  openEdit(inv: CustomerInvoiceListItemDto): void {
    this.editingId = inv.id;
    this.dialogError = '';
    this.dialogMode = 'edit';
    this.form.enable();
    this.lines.clear();
    this.selectedSo = null;
    this.dialogVisible = true;

    this.invService.getById(inv.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const c = res.data;
            this.dialogMode = c.status === 'Draft' ? 'edit' : 'view';
            this.form.patchValue({
              salesOrderId: c.salesOrderId,
              vatRatePercent: Math.round((c.vatRate || 0) * 10000) / 100,
              invoiceDate: c.invoiceDate,
              dueDate: c.dueDate,
              notes: c.notes ?? ''
            });
            for (const l of c.lines) {
              this.lines.push(this.newLine(
                l.productId,
                `${l.productCode} — ${l.productName}`,
                l.unitOfMeasureCode,
                l.quantity,
                l.unitPrice,
                l.lineNotes ?? ''
              ));
            }
            this.form.get('salesOrderId')?.disable();
            if (this.dialogMode === 'view') this.form.disable();
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving || this.dialogMode === 'view') return;

    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();
    const lines = (v.lines as any[]).map(l => ({
      productId: l.productId,
      quantity: Number(l.quantity) || 0,
      unitPrice: Number(l.unitPrice) || 0,
      lineNotes: (l.lineNotes as string)?.trim() || null
    }));

    if (this.dialogMode === 'create') {
      this.invService.create({
        salesOrderId: v.salesOrderId,
        vatRate: this.vatRateDecimal,
        invoiceDate: v.invoiceDate,
        dueDate: v.dueDate || null,
        notes: (v.notes as string)?.trim() || null,
        lines
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.invService.update(this.editingId, {
        vatRate: this.vatRateDecimal,
        invoiceDate: v.invoiceDate,
        dueDate: v.dueDate || v.invoiceDate,
        notes: (v.notes as string)?.trim() || null,
        lines
      }).subscribe({
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

  issue(inv: CustomerInvoiceListItemDto): void {
    this.runRowAction(inv, this.invService.issue.bind(this.invService));
  }

  cancel(inv: CustomerInvoiceListItemDto): void {
    this.runRowAction(inv, this.invService.cancel.bind(this.invService));
  }

  private runRowAction(inv: CustomerInvoiceListItemDto, fn: (id: number) => any): void {
    if (this.rowActionId) return;
    this.rowActionId = inv.id;
    this.actionError = '';
    this.cdr.detectChanges();
    fn(inv.id).subscribe({
      next: (res: { success: boolean; message?: string | null }) => this.handleRowAction(res),
      error: (err: any) => this.handleRowActionError(err)
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

  canCancel(inv: CustomerInvoiceListItemDto): boolean {
    return (inv.status === 'Draft' || inv.status === 'Issued') && inv.amountPaid === 0;
  }

  canDelete(inv: CustomerInvoiceListItemDto): boolean {
    return inv.status === 'Draft' || inv.status === 'Cancelled';
  }

  confirmDelete(inv: CustomerInvoiceListItemDto): void {
    this.deletingInv = inv;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingInv || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.invService.delete(this.deletingInv.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingInv = null;
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

  lineTotal(line: AbstractControl): number {
    const qty = Number(line.get('quantity')?.value) || 0;
    const price = Number(line.get('unitPrice')?.value) || 0;
    return qty * price;
  }

  meta(line: AbstractControl) {
    return line.getRawValue();
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency', currency: 'BDT', maximumFractionDigits: 2
    }).format(amount || 0);
  }

  /** Format an amount in a given ISO currency code (falls back to plain + code). */
  formatMoney(amount: number, code: string | null | undefined): string {
    const c = code || 'BDT';
    try {
      return new Intl.NumberFormat('en-US', {
        style: 'currency', currency: c, maximumFractionDigits: 2
      }).format(amount || 0);
    } catch {
      return `${(amount || 0).toLocaleString('en-US', { maximumFractionDigits: 2 })} ${c}`;
    }
  }
}
