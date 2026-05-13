import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: false,
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent implements OnInit {
  form!: FormGroup;
  loading = false;
  sent = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]]
    });
  }

  onSubmit(): void {
    if (this.form.invalid || this.loading) return;

    this.loading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    const { email } = this.form.value;

    this.authService.forgotPassword(email).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success) {
            this.sent = true;
          } else {
            this.errorMessage = res.message || 'Request failed. Please try again.';
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
