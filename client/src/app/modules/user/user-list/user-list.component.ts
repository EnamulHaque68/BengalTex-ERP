import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { UserService } from '../../../services/user.service';
import { RoleService } from '../../../services/role.service';
import {
  PagedQueryParameters,
  RoleListItemDto,
  UserDto,
  UserListItemDto
} from '../../../models/user.models';

@Component({
  selector: 'app-user-list',
  standalone: false,
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss'
})
export class UserListComponent implements OnInit {

  // List state
  users: UserListItemDto[] = [];
  loading = false;
  totalCount = 0;

  // Paging / search
  parameters: PagedQueryParameters = { page: 1, pageSize: 25, search: '' };
  searchTimer: any = null;

  // Roles for picker
  allRoles: RoleListItemDto[] = [];

  // ── Create/Edit dialog ─────────────────────────────────────────────
  dialogVisible = false;
  dialogMode: 'create' | 'edit' = 'create';
  dialogSaving = false;
  dialogError = '';
  editingId: string | null = null;
  form!: FormGroup;

  // ── Roles dialog ───────────────────────────────────────────────────
  rolesDialogVisible = false;
  rolesDialogSaving = false;
  rolesDialogError = '';
  rolesEditingUser: UserListItemDto | null = null;
  selectedRoles: string[] = [];

  // ── Reset password dialog ──────────────────────────────────────────
  resetPwDialogVisible = false;
  resetPwSaving = false;
  resetPwError = '';
  resetPwUser: UserListItemDto | null = null;
  resetPwForm!: FormGroup;

  // ── Deactivate confirm ─────────────────────────────────────────────
  toggleDialogVisible = false;
  toggleUser: UserListItemDto | null = null;
  toggleSaving = false;

  constructor(
    private userService: UserService,
    private roleService: RoleService,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForms();
    this.loadRoles();
    this.loadUsers();
  }

  // ── Form definitions ───────────────────────────────────────────────

  private buildForms(): void {
    this.form = this.fb.group({
      userName: ['', [Validators.required, Validators.maxLength(100),
        Validators.pattern(/^[a-zA-Z0-9_.-]+$/)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      factoryId: [null],
      // create-only fields
      password: ['', [Validators.minLength(8), Validators.pattern(/^(?=.*[A-Z])(?=.*[0-9]).+$/)]],
      confirmPassword: [''],
      roles: [[] as string[]]
    });

    this.resetPwForm = this.fb.group({
      newPassword: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[A-Z])(?=.*[0-9]).+$/)
      ]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordsMatchValidator });
  }

  private passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
    const np = group.get('newPassword')?.value;
    const cp = group.get('confirmPassword')?.value;
    return np && cp && np !== cp ? { passwordsMismatch: true } : null;
  }

  // ── Roles dropdown source ──────────────────────────────────────────

  private loadRoles(): void {
    this.roleService.getAll().subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success) this.allRoles = res.data ?? [];
          this.cdr.detectChanges();
        });
      }
    });
  }

  // ── List loading ───────────────────────────────────────────────────

  loadUsers(): void {
    this.loading = true;
    this.userService.getAll(this.parameters).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success && res.data) {
            this.users = res.data.items;
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
    // Debounce 400ms — avoid hammering API on every keystroke
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.parameters.search = value;
      this.parameters.page = 1;
      this.loadUsers();
    }, 400);
  }

  onPageChange(event: any): void {
    this.parameters.page = Math.floor(event.first / event.rows) + 1;
    this.parameters.pageSize = event.rows;
    this.loadUsers();
  }

  // ── Create / Edit dialog ───────────────────────────────────────────

  openCreate(): void {
    this.dialogMode = 'create';
    this.editingId = null;
    this.dialogError = '';
    this.form.reset({
      userName: '', email: '', fullName: '', factoryId: null,
      password: '', confirmPassword: '', roles: []
    });
    // Re-apply required validators for create
    this.form.get('password')?.addValidators(Validators.required);
    this.form.get('confirmPassword')?.addValidators(Validators.required);
    this.form.get('password')?.updateValueAndValidity();
    this.form.get('confirmPassword')?.updateValueAndValidity();
    this.dialogVisible = true;
  }

  openEdit(id: string): void {
    this.dialogMode = 'edit';
    this.editingId = id;
    this.dialogError = '';
    this.dialogVisible = true;

    // Password fields not used in edit mode — clear their required validators
    this.form.get('password')?.clearValidators();
    this.form.get('confirmPassword')?.clearValidators();
    this.form.get('password')?.updateValueAndValidity();
    this.form.get('confirmPassword')?.updateValueAndValidity();

    this.userService.getById(id).subscribe({
      next: (res) => {
        this.zone.run(() => {
          if (res.success && res.data) {
            const u = res.data;
            this.form.patchValue({
              userName: u.userName,
              email: u.email,
              fullName: u.fullName,
              factoryId: u.factoryId,
              roles: u.roles ?? []
            });
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  saveUser(): void {
    if (this.form.invalid || this.dialogSaving) return;

    this.dialogSaving = true;
    this.dialogError = '';
    this.cdr.detectChanges();

    const v = this.form.value;

    if (this.dialogMode === 'create') {
      this.userService.create({
        userName: v.userName,
        email: v.email,
        fullName: v.fullName,
        password: v.password,
        confirmPassword: v.confirmPassword,
        factoryId: v.factoryId,
        roles: v.roles ?? []
      }).subscribe({
        next: (res) => this.handleSaveResult(res),
        error: (err) => this.handleSaveError(err)
      });
    } else if (this.editingId) {
      this.userService.update(this.editingId, {
        userName: v.userName,
        email: v.email,
        fullName: v.fullName,
        factoryId: v.factoryId
      }).subscribe({
        next: (res) => this.handleSaveResult(res),
        error: (err) => this.handleSaveError(err)
      });
    }
  }

  private handleSaveResult(res: any): void {
    this.zone.run(() => {
      this.dialogSaving = false;
      if (res.success) {
        this.dialogVisible = false;
        this.loadUsers();
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

  // ── Roles dialog ───────────────────────────────────────────────────

  openRolesDialog(user: UserListItemDto): void {
    this.rolesEditingUser = user;
    this.selectedRoles = [...user.roles];
    this.rolesDialogError = '';
    this.rolesDialogVisible = true;
  }

  saveRoles(): void {
    if (!this.rolesEditingUser || this.rolesDialogSaving) return;
    this.rolesDialogSaving = true;
    this.rolesDialogError = '';
    this.cdr.detectChanges();

    this.userService.updateRoles(this.rolesEditingUser.id, this.selectedRoles).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.rolesDialogSaving = false;
          if (res.success) {
            this.rolesDialogVisible = false;
            this.loadUsers();
          } else {
            this.rolesDialogError = res.message || 'Save failed.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.rolesDialogSaving = false;
          this.rolesDialogError = err?.error?.message || 'Save failed.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  toggleRoleSelection(roleName: string): void {
    const idx = this.selectedRoles.indexOf(roleName);
    if (idx >= 0) this.selectedRoles.splice(idx, 1);
    else this.selectedRoles.push(roleName);
  }

  toggleFormRole(roleName: string, checked: boolean): void {
    const current: string[] = this.form.get('roles')?.value ?? [];
    if (checked && !current.includes(roleName)) {
      this.form.get('roles')?.setValue([...current, roleName]);
    } else if (!checked) {
      this.form.get('roles')?.setValue(current.filter(x => x !== roleName));
    }
  }

  isRoleSelectedInForm(roleName: string): boolean {
    const value = this.form.get('roles')?.value as string[] | null;
    return value?.includes(roleName) ?? false;
  }

  // ── Reset password dialog ──────────────────────────────────────────

  openResetPassword(user: UserListItemDto): void {
    this.resetPwUser = user;
    this.resetPwError = '';
    this.resetPwForm.reset();
    this.resetPwDialogVisible = true;
  }

  saveResetPassword(): void {
    if (!this.resetPwUser || this.resetPwForm.invalid || this.resetPwSaving) return;
    this.resetPwSaving = true;
    this.resetPwError = '';
    this.cdr.detectChanges();

    const { newPassword, confirmPassword } = this.resetPwForm.value;

    this.userService.resetPassword(this.resetPwUser.id, newPassword, confirmPassword).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.resetPwSaving = false;
          if (res.success) {
            this.resetPwDialogVisible = false;
          } else {
            this.resetPwError = res.message || 'Reset failed.';
          }
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        this.zone.run(() => {
          this.resetPwSaving = false;
          this.resetPwError = err?.error?.message || 'Reset failed.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  // ── Activate / deactivate ──────────────────────────────────────────

  confirmToggleActive(user: UserListItemDto): void {
    this.toggleUser = user;
    this.toggleDialogVisible = true;
  }

  doToggleActive(): void {
    if (!this.toggleUser || this.toggleSaving) return;
    this.toggleSaving = true;
    this.cdr.detectChanges();

    const newActive = !this.toggleUser.isActive;

    this.userService.setActive(this.toggleUser.id, newActive).subscribe({
      next: () => {
        this.zone.run(() => {
          this.toggleSaving = false;
          this.toggleDialogVisible = false;
          this.toggleUser = null;
          this.loadUsers();
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => {
          this.toggleSaving = false;
          this.cdr.detectChanges();
        });
      }
    });
  }
}
