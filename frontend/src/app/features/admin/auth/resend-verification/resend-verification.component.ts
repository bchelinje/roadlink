import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { UsersService } from '@core/api';

@Component({
  selector: 'app-resend-verification',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './resend-verification.component.html',
  styleUrls: ['./resend-verification.component.scss']
})
export class ResendVerificationComponent {
  private readonly fb = inject(FormBuilder);
  private readonly usersService = inject(UsersService);
  private readonly router = inject(Router);

  resendForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  emailSent = false;

  constructor() {
    this.resendForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  get email() {
    return this.resendForm.get('email');
  }

  onSubmit(): void {
    if (this.resendForm.invalid) {
      this.email?.markAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const { email } = this.resendForm.value;

    this.usersService.apiUsersResendConfirmationPost({ email })
      .subscribe({
        next: (response: any) => {
          this.successMessage = 'Verification email has been sent. Please check your inbox.';
          this.emailSent = true;
          this.isLoading = false;
          this.resendForm.reset();
        },
        error: (error) => {
          console.error('Resend verification error:', error);
          // Don't reveal if email exists for security
          this.successMessage = 'If an account exists with this email, you will receive a verification link.';
          this.emailSent = true;
          this.isLoading = false;
        }
      });
  }

  backToLogin(): void {
    this.router.navigate(['/login']);
  }

  tryAnotherEmail(): void {
    this.emailSent = false;
    this.successMessage = '';
    this.errorMessage = '';
  }
}
