import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { WarehouseService } from '../../../services/warehouse.service';
import { FactoryService } from '../../../services/company.service';
import {
  WAREHOUSE_TYPES,
  WarehouseDto,
  WarehouseTypeName
} from '../../../models/master-data.models';
import { FactoryListItemDto } from '../../../models/company.models';

@Component({
  selector: 'app-warehouse-list',
  standalone: false,
  templateUrl: './warehouse-list.component.html',
  styleUrl: './warehouse-list.component.scss'
})
export class WarehouseListComponent implements OnInit {

  warehouses: WarehouseDto[] = [];
  factories: FactoryListItemDto[] = [];
  loading = false;
  includeInactive = false;
  filterFactoryId: number | null = null;

  warehouseTypes = WAREHOUSE_TYPES;

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingWarehouse: WarehouseDto | null = null;
  deleting = false;
  deleteError = '';

  constructor(
    private warehouseService: WarehouseService,
    private factoryService: FactoryService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadFactories();
    this.load();
  }

  private buildForm(): void {
    this.form = this.fb.group({
      code: ['', [Validators.required, Validators.pattern(/^[A-Z0-9_-]+$/), Validators.maxLength(20)]],
      name: ['', [Validators.required, Validators.maxLength(100)]],
      warehouseType: ['General' as WarehouseTypeName, Validators.required],
      address: ['', Validators.maxLength(300)],
      factoryId: [null as number | null, Validators.required],
      isActive: [true]
    });
  }

  private loadFactories(): void {
    this.factoryService.getAll(false).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success) this.factories = res.data ?? [];
          this.cdr.detectChanges();
        });
      }
    });
  }

  load(): void {
    this.loading = true;
    this.warehouseService.getAll(this.filterFactoryId ?? undefined, this.includeInactive).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          this.warehouses = res.data ?? [];
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); });
      }
    });
  }

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.reset({
      code: '', name: '', warehouseType: 'General', address: '',
      factoryId: this.filterFactoryId ?? this.factories[0]?.id ?? null,
      isActive: true
    });
    this.form.get('code')?.enable();
    this.form.get('factoryId')?.enable();
    this.dialogVisible = true;
  }

  openEdit(id: number): void {
    this.dialogMode = 'edit';
    this.editingId = id;
    this.dialogError = '';
    this.dialogVisible = true;

    this.warehouseService.getById(id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const w = res.data;
            this.form.patchValue({
              code: w.code,
              name: w.name,
              warehouseType: w.warehouseType,
              address: w.address ?? '',
              factoryId: w.factoryId,
              isActive: w.isActive
            });
            // Code and FactoryId are identity — locked after creation
            this.form.get('code')?.disable();
            this.form.get('factoryId')?.disable();
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  save(): void {
    if (this.form.invalid || this.dialogSaving) return;

    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.getRawValue();

    if (this.dialogMode === 'create') {
      this.warehouseService.create({
        code: (v.code as string).toUpperCase(),
        name: v.name,
        warehouseType: v.warehouseType,
        address: v.address || null,
        factoryId: v.factoryId
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.warehouseService.update(this.editingId, {
        name: v.name,
        warehouseType: v.warehouseType,
        address: v.address || null,
        isActive: v.isActive
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

  confirmDelete(warehouse: WarehouseDto): void {
    this.deletingWarehouse = warehouse;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingWarehouse || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.warehouseService.delete(this.deletingWarehouse.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingWarehouse = null;
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
}
