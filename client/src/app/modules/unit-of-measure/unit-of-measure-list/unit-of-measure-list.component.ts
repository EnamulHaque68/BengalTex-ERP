import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { UnitOfMeasureService } from '../../../services/unit-of-measure.service';
import {
  UNIT_TYPES,
  UnitOfMeasureDto,
  UnitTypeName
} from '../../../models/master-data.models';

@Component({
  selector: 'app-unit-of-measure-list',
  standalone: false,
  templateUrl: './unit-of-measure-list.component.html',
  styleUrl: './unit-of-measure-list.component.scss'
})
export class UnitOfMeasureListComponent implements OnInit {

  units: UnitOfMeasureDto[] = [];
  loading = false;
  includeInactive = false;

  unitTypes = UNIT_TYPES;

  // Dialog
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: number | null = null;
  form!: FormGroup;

  // Delete
  deleteDialogVisible = false;
  deletingUnit: UnitOfMeasureDto | null = null;
  deleting = false;
  deleteError = '';

  constructor(
    private uomService: UnitOfMeasureService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.load();
  }

  private buildForm(): void {
    this.form = this.fb.group({
      code: ['', [Validators.required, Validators.pattern(/^[A-Z0-9]+$/), Validators.maxLength(10)]],
      name: ['', [Validators.required, Validators.maxLength(50)]],
      symbol: ['', [Validators.required, Validators.maxLength(10)]],
      unitType: ['Count' as UnitTypeName, Validators.required],
      baseUnitId: [null as number | null],
      conversionFactor: [1, [Validators.required, Validators.min(0.000001)]],
      isActive: [true]
    });
  }

  /** Possible base units for the dropdown — same type, no derivative-of-derivative. */
  get baseUnitOptions(): UnitOfMeasureDto[] {
    const selectedType = this.form?.get('unitType')?.value as UnitTypeName;
    return this.units.filter(u =>
      u.unitType === selectedType &&
      u.baseUnitId === null &&
      u.id !== this.editingId
    );
  }

  load(): void {
    this.loading = true;
    this.uomService.getAll(this.includeInactive).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          this.units = res.data ?? [];
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
      code: '', name: '', symbol: '',
      unitType: 'Count', baseUnitId: null,
      conversionFactor: 1, isActive: true
    });
    // All fields editable in create
    this.form.get('code')?.enable();
    this.form.get('unitType')?.enable();
    this.form.get('baseUnitId')?.enable();
    this.dialogVisible = true;
  }

  openEdit(id: number): void {
    this.dialogMode = 'edit';
    this.editingId = id;
    this.dialogError = '';
    this.dialogVisible = true;

    this.uomService.getById(id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const u = res.data;
            this.form.patchValue({
              code: u.code,
              name: u.name,
              symbol: u.symbol,
              unitType: u.unitType,
              baseUnitId: u.baseUnitId,
              conversionFactor: u.conversionFactor,
              isActive: u.isActive
            });
            // Code / UnitType / BaseUnit are identity — locked after creation
            this.form.get('code')?.disable();
            this.form.get('unitType')?.disable();
            this.form.get('baseUnitId')?.disable();
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
      this.uomService.create({
        code: (v.code as string).toUpperCase(),
        name: v.name,
        symbol: v.symbol,
        unitType: v.unitType,
        baseUnitId: v.baseUnitId || null,
        conversionFactor: v.conversionFactor
      }).subscribe({
        next: (res) => this.handleSave(res),
        error: (err) => this.handleError(err)
      });
    } else if (this.editingId) {
      this.uomService.update(this.editingId, {
        name: v.name,
        symbol: v.symbol,
        conversionFactor: v.conversionFactor,
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

  confirmDelete(unit: UnitOfMeasureDto): void {
    this.deletingUnit = unit;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingUnit || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.uomService.delete(this.deletingUnit.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingUnit = null;
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
