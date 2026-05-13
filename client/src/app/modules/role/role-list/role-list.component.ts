import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RoleService } from '../../../services/role.service';
import { PermissionService } from '../../../services/permission.service';
import { RoleDto, RoleListItemDto } from '../../../models/user.models';
import { PermissionGroupDto } from '../../../models/permission.models';

@Component({
  selector: 'app-role-list',
  standalone: false,
  templateUrl: './role-list.component.html',
  styleUrl: './role-list.component.scss'
})
export class RoleListComponent implements OnInit {

  // List state
  roles: RoleListItemDto[] = [];
  loading = false;

  // Catalog of all defined permissions (for the picker)
  permissionGroups: PermissionGroupDto[] = [];

  // ── Create / Edit Role dialog ──────────────────────────────────────
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: string | null = null;
  editingIsSystem = false;
  form!: FormGroup;

  // ── Permissions Picker dialog ──────────────────────────────────────
  permsDialogVisible = false;
  permsDialogSaving = false;
  permsDialogError = '';
  permsRole: RoleListItemDto | null = null;
  selectedPermissions = new Set<string>();

  // ── Delete confirm ─────────────────────────────────────────────────
  deleteDialogVisible = false;
  deletingRole: RoleListItemDto | null = null;
  deleting = false;
  deleteError = '';

  constructor(
    private roleService: RoleService,
    private permissionService: PermissionService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadRoles();
    this.loadPermissions();
  }

  private buildForm(): void {
    this.form = this.fb.group({
      name: ['', [
        Validators.required,
        Validators.maxLength(100),
        Validators.pattern(/^[a-zA-Z][a-zA-Z0-9_ -]*$/)
      ]],
      description: ['', [Validators.maxLength(500)]]
    });
  }

  loadRoles(): void {
    this.loading = true;
    this.roleService.getAll().subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          this.roles = res.data ?? [];
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => { this.loading = false; this.cdr.detectChanges(); });
      }
    });
  }

  private loadPermissions(): void {
    this.permissionService.getAll().subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success) this.permissionGroups = res.data ?? [];
          this.cdr.detectChanges();
        });
      }
    });
  }

  // ── Create / Edit ──────────────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.editingIsSystem = false;
    this.dialogError = '';
    this.form.reset({ name: '', description: '' });
    this.form.enable();
    this.dialogVisible = true;
  }

  openEdit(role: RoleListItemDto): void {
    this.dialogMode = 'edit';
    this.editingId = role.id;
    this.editingIsSystem = role.isSystemRole;
    this.dialogError = '';
    this.form.patchValue({ name: role.name, description: role.description ?? '' });

    if (role.isSystemRole) {
      this.form.disable();
      this.dialogError = 'System roles are read-only and cannot be edited.';
    } else {
      this.form.enable();
    }
    this.dialogVisible = true;
  }

  saveRole(): void {
    if (this.form.invalid || this.dialogSaving || this.editingIsSystem) return;
    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const { name, description } = this.form.value;
    const desc = description?.trim() || null;

    // Two separate subscriptions — create() and update() have different result-type
    // generics (string vs null), so the TS-merged subscribe signature can't unify.
    if (this.dialogMode === 'create') {
      this.roleService.create(name, desc).subscribe({
        next: (res) => this.handleSaveResult(res),
        error: (err) => this.handleSaveError(err)
      });
    } else if (this.editingId) {
      this.roleService.update(this.editingId, name, desc).subscribe({
        next: (res) => this.handleSaveResult(res),
        error: (err) => this.handleSaveError(err)
      });
    }
  }

  private handleSaveResult(res: { success: boolean; message?: string | null }): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      if (res.success) {
        this.dialogVisible = false;
        this.loadRoles();
      } else {
        this.dialogError = res.message || 'Save failed.';
      }
      this.cdr.detectChanges();
    });
  }

  private handleSaveError(err: any): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      this.dialogError = err?.error?.message || 'Save failed.';
      this.cdr.detectChanges();
    });
  }

  // ── Permissions Picker ─────────────────────────────────────────────

  openPermissions(role: RoleListItemDto): void {
    this.permsRole = role;
    this.permsDialogError = '';
    this.permsDialogVisible = true;

    // Fetch current role permissions so we can preselect them
    this.roleService.getById(role.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            this.selectedPermissions = new Set(res.data.permissions);
          } else {
            this.selectedPermissions = new Set();
          }
          this.cdr.detectChanges();
        });
      }
    });
  }

  togglePermission(key: string): void {
    if (this.selectedPermissions.has(key)) {
      this.selectedPermissions.delete(key);
    } else {
      this.selectedPermissions.add(key);
    }
  }

  isAllSelectedInGroup(group: PermissionGroupDto): boolean {
    return group.permissions.every(p => this.selectedPermissions.has(p.key));
  }

  isAnySelectedInGroup(group: PermissionGroupDto): boolean {
    return group.permissions.some(p => this.selectedPermissions.has(p.key));
  }

  toggleAllInGroup(group: PermissionGroupDto): void {
    const all = this.isAllSelectedInGroup(group);
    if (all) {
      group.permissions.forEach(p => this.selectedPermissions.delete(p.key));
    } else {
      group.permissions.forEach(p => this.selectedPermissions.add(p.key));
    }
  }

  savePermissions(): void {
    if (!this.permsRole || this.permsDialogSaving) return;
    this.permsDialogSaving = true;
    this.permsDialogError = '';
    this.cdr.detectChanges();

    this.roleService.updatePermissions(this.permsRole.id, Array.from(this.selectedPermissions))
      .subscribe({
        next: (res) => {
          this.zone.run(() => {
            this.permsDialogSaving = false;
            if (res.success) {
              this.permsDialogVisible = false;
              this.loadRoles();
            } else {
              this.permsDialogError = res.message || 'Save failed.';
            }
            this.cdr.detectChanges();
          });
        },
        error: (err) => {
          this.zone.run(() => {
            this.permsDialogSaving = false;
            this.permsDialogError = err?.error?.message || 'Save failed.';
            this.cdr.detectChanges();
          });
        }
      });
  }

  selectedCountInGroup(group: PermissionGroupDto): number {
    return group.permissions.filter(p => this.selectedPermissions.has(p.key)).length;
  }

  // ── Delete ─────────────────────────────────────────────────────────

  confirmDelete(role: RoleListItemDto): void {
    this.deletingRole = role;
    this.deleteError = '';
    this.deleteDialogVisible = true;
  }

  doDelete(): void {
    if (!this.deletingRole || this.deleting) return;
    this.deleting = true;
    this.deleteError = '';
    this.cdr.detectChanges();

    this.roleService.delete(this.deletingRole.id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.deleting = false;
          if (res.success) {
            this.deleteDialogVisible = false;
            this.deletingRole = null;
            this.loadRoles();
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
