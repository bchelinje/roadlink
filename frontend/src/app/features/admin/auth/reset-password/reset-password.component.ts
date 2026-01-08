import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormControl, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { UsersService } from '@core/api';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.scss']
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly usersService = inject(UsersService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  resetPasswordForm!: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  showNewPassword = false;
  showConfirmPassword = false;
  resetComplete = false;

  email = '';
  token = '';

  ngOnInit(): void {
    // Get email and token from query params
    this.route.queryParams.subscribe(params => {
      this.email = params['email'] || '';
      this.token = params['token'] || '';

      if (!this.email || !this.token) {
        this.errorMessage = 'Invalid reset link. Please request a new password reset.';
      }
    });

    this.initializeForm();
  }

  private initializeForm(): void {
    this.resetPasswordForm = this.fb.group({
      newPassword: ['', [
        Validators.required,
        Validators.minLength(8),
        this.passwordStrengthValidator
      ]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  private passwordStrengthValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (!value) return null;

    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasNumeric = /[0-9]/.test(value);
    const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(value);

    const passwordValid = hasUpperCase && hasLowerCase && hasNumeric && hasSpecialChar;

    return passwordValid ? null : { passwordStrength: true };
  }

  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword');
    const confirmPassword = control.get('confirmPassword');

    if (!newPassword || !confirmPassword) return null;

    return newPassword.value === confirmPassword.value ? null : { passwordMismatch: true };
  }

  get newPassword(): FormControl {
    return this.resetPasswordForm.get('newPassword') as FormControl;
  }

  get confirmPassword(): FormControl {
    return this.resetPasswordForm.get('confirmPassword') as FormControl;
  }

  toggleNewPasswordVisibility(): void {
    this.showNewPassword = !this.showNewPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  onSubmit(): void {
    if (this.resetPasswordForm.invalid) {
      this.markFormGroupTouched(this.resetPasswordForm);
      return;
    }

    if (!this.email || !this.token) {
      this.errorMessage = 'Invalid reset link. Please request a new password reset.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const { newPassword } = this.resetPasswordForm.value;

    this.usersService.apiUsersResetPasswordPost({
      email: this.email,
      token: this.token,
      newPassword: newPassword
    }).subscribe({
      next: (response: any) => {
        this.successMessage = 'Password reset successful!';
        this.resetComplete = true;
        this.isLoading = false;

        // Redirect to login after 3 seconds
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 3000);
      },
      error: (error) => {
        console.error('Reset password error:', error);
        this.errorMessage = error.error?.message || 'Failed to reset password. The link may have expired.';
        this.isLoading = false;
      }
    });
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  getPasswordLength(): number {
    return this.newPassword?.value?.length ?? 0;
  }

  hasUpperCase(): boolean {
    return /[A-Z]/.test(this.newPassword?.value ?? '');
  }

  hasLowerCase(): boolean {
    return /[a-z]/.test(this.newPassword?.value ?? '');
  }

  hasNumber(): boolean {
    return /[0-9]/.test(this.newPassword?.value ?? '');
  }

  hasSpecialChar(): boolean {
    const value = this.newPassword?.value ?? '';
    return /[!@#$%^&*(),.?":{}|<>]/.test(value);
  }

  backToLogin(): void {
    this.router.navigate(['/login']);
  }

  requestNewLink(): void {
    this.router.navigate(['/forgot-password']);
  }
}
