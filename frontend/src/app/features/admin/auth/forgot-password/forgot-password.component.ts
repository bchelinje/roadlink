import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { UsersService } from '@core/api';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly usersService = inject(UsersService);
  private readonly router = inject(Router);

  forgotPasswordForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  emailSent = false;

  constructor() {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  get email() {
    return this.forgotPasswordForm.get('email');
  }

  onSubmit(): void {
    if (this.forgotPasswordForm.invalid) {
      this.email?.markAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const { email } = this.forgotPasswordForm.value;

    this.usersService.apiUsersForgotPasswordPost({ email: email })
      .subscribe({
        next: (response: any) => {
          this.successMessage = 'Password reset instructions have been sent to your email.';
          this.emailSent = true;
          this.isLoading = false;
          this.forgotPasswordForm.reset();
        },
        error: (error) => {
          console.error('Forgot password error:', error);
          // Don't reveal if email exists for security
          this.successMessage = 'If an account exists with this email, you will receive password reset instructions.';
          this.emailSent = true;
          this.isLoading = false;
        }
      });
  }

  backToLogin(): void {
    this.router.navigate(['/login']);
  }

  resendEmail(): void {
    this.emailSent = false;
    this.successMessage = '';
    this.errorMessage = '';
  }
}
