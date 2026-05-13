import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: false,
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent implements OnInit {
  form!: FormGroup;
  loading = false;
  success = false;
  errorMessage = '';
  email = '';
  invalidLink = false;

  private token = '';

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Browser auto-decodes query params — token is in its raw (unencoded) form here
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';

    if (!this.email || !this.token) {
      this.invalidLink = true;
      this.errorMessage = 'Invalid or missing reset link. Please request a new one.';
    }

    this.form = this.fb.group(
      {
        newPassword: [
          '',
          [
            Validators.required,
            Validators.minLength(8),
            // At least one uppercase letter AND one digit
            Validators.pattern(/^(?=.*[A-Z])(?=.*[0-9]).+$/)
          ]
        ],
        confirmPassword: ['', Validators.required]
      },
      { validators: this.passwordsMatchValidator }
    );
  }

  private passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
    const np = group.get('newPassword')?.value;
    const cp = group.get('confirmPassword')?.value;
    return np && cp && np !== cp ? { passwordsMismatch: true } : null;
  }

  onSubmit(): void {
    if (this.form.invalid || this.loading || this.invalidLink) return;

    this.loading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    const { newPassword, confirmPassword } = this.form.value;

    this.authService
      .resetPassword(this.email, this.token, newPassword, confirmPassword)
      .subscribe({
        next: (res) => {
          this.zone.run(() => {
            this.loading = false;
            if (res.success) {
              this.success = true;
              setTimeout(() => this.router.navigate(['/login']), 3000);
            } else {
              this.errorMessage = res.message || 'Reset failed. Please try again.';
            }
            this.cdr.detectChanges();
          });
        },
        error: (err) => {
          this.zone.run(() => {
            this.loading = false;
            this.errorMessage = err?.error?.message || 'Unable to connect. Please try again.';
            this.cdr.detectChanges();
          });
        }
      });
  }
}
