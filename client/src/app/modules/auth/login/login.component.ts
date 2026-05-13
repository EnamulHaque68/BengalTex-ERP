import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  loading = false;
  errorMessage = '';
  private deviceFingerprint: string | undefined;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Redirect if already logged in
    if (this.authService.isAuthenticated) {
      this.router.navigate(['/']);
      return;
    }

    this.loginForm = this.fb.group({
      emailOrUsername: ['', [Validators.required, Validators.maxLength(256)]],
      password: ['', [Validators.required, Validators.maxLength(128)]]
    });

    this.loadFingerprint();
  }

  private loadFingerprint(): void {
    // FingerprintJS OSS — async load, non-blocking
    import('@fingerprintjs/fingerprintjs').then(FingerprintJS => {
      FingerprintJS.load().then(fp => fp.get()).then(result => {
        this.deviceFingerprint = result.visitorId;
      }).catch(() => {
        // Fingerprint is optional — login proceeds without it
      });
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid || this.loading) return;

    this.loading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    const { emailOrUsername, password } = this.loginForm.value;

    this.authService.login(emailOrUsername, password, this.deviceFingerprint).subscribe({
      next: (res) => {
        this.zone.run(() => {
          this.loading = false;
          if (res.success) {
            this.router.navigate(['/']);
          } else {
            this.errorMessage = res.message || 'Login failed.';
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
