import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors,
  FormControl
} from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '@core/services/auth.service';
import { UsersService, UpdateUserModel } from '@core/api';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly usersService = inject(UsersService);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  private destroy$ = new Subject<void>();

  currentUser: any = null;
  isLoading = true;
  isSaving = false;

  // Active tab
  activeTab: 'profile' | 'password' | 'settings' = 'profile';

  // Forms
  profileForm!: FormGroup;
  passwordForm!: FormGroup;

  // Messages
  successMessage = '';
  errorMessage = '';

  // Profile picture
  selectedFile: File | null = null;
  previewUrl: string | null = null;

  ngOnInit(): void {
    this.initializeForms();
    this.loadCurrentUser();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForms(): void {
    // Profile form
    this.profileForm = this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.pattern(/^[\d\s\-\+\(\)]+$/)]],
    });

    // Password form
    this.passwordForm = this.fb.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [
        Validators.required,
        Validators.minLength(8),
        this.passwordStrengthValidator
      ]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  private loadCurrentUser(): void {
    this.isLoading = true;

    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (user) => {
          if (user) {
            this.currentUser = user;
            this.patchProfileForm();
          }
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading user:', error);
          this.errorMessage = 'Failed to load user profile';
          this.isLoading = false;
        }
      });
  }

   patchProfileForm(): void {
    this.profileForm.patchValue({
      userName: this.currentUser.name || this.currentUser.userName,
      email: this.currentUser.email,
      phoneNumber: this.currentUser.phoneNumber || ''
    });
  }

  // Tab switching
  switchTab(tab: 'profile' | 'password' | 'settings'): void {
    this.activeTab = tab;
    this.clearMessages();
  }

  // Profile update
  onProfileSubmit(): void {
    if (this.profileForm.invalid) {
      this.markFormGroupTouched(this.profileForm);
      return;
    }

    this.isSaving = true;
    this.clearMessages();

    const updateData: UpdateUserModel = {
      userName: this.profileForm.value.userName,
      email: this.profileForm.value.email,
      phoneNumber: this.profileForm.value.phoneNumber || null
    };

    this.usersService.apiUsersIdPut(this.currentUser.id, updateData)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.successMessage = 'Profile updated successfully!';
          this.isSaving = false;

          // Refresh user data
          this.loadCurrentUser();
        },
        error: (error) => {
          console.error('Error updating profile:', error);
          this.errorMessage = error.error?.message || 'Failed to update profile';
          this.isSaving = false;
        }
      });
  }

  // Password change
  onPasswordSubmit(): void {
    if (this.passwordForm.invalid) {
      this.markFormGroupTouched(this.passwordForm);
      return;
    }

    this.isSaving = true;
    this.clearMessages();

    const { currentPassword, newPassword } = this.passwordForm.value;

    this.usersService.apiUsersChangePasswordPost({
      currentPassword,
      newPassword
    }).subscribe({
      next: () => {
        this.successMessage = 'Password changed successfully!';
        this.toastService.success('Success', 'Password changed successfully!');
        this.isSaving = false;
        this.passwordForm.reset();
      },
      error: (error) => {
        console.error('Error changing password:', error);
        this.errorMessage = error.error?.message || 'Failed to change password';
        this.toastService.error('Error', this.errorMessage);
        this.isSaving = false;
      }
    });
  }

  // Profile picture handling
  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      // Validate file
      if (!file.type.startsWith('image/')) {
        this.errorMessage = 'Please select a valid image file';
        return;
      }

      if (file.size > 5 * 1024 * 1024) { // 5MB limit
        this.errorMessage = 'Image size should be less than 5MB';
        return;
      }

      this.selectedFile = file;

      // Create preview
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.previewUrl = e.target.result;
      };
      reader.readAsDataURL(file);
    }
  }

  uploadProfilePicture(): void {
    if (!this.selectedFile || !this.currentUser?.id) return;

    this.isSaving = true;
    this.clearMessages();

    this.usersService.apiUsersIdProfilePicturePost(this.currentUser.id, this.selectedFile).subscribe({
      next: (response) => {
        this.successMessage = 'Profile picture updated successfully!';
        this.toastService.success('Success', 'Profile picture updated successfully!');
        if (response.url) {
          this.currentUser.profilePictureUrl = response.url;
        }
        this.isSaving = false;
        this.selectedFile = null;
      },
      error: (error) => {
        console.error('Error uploading profile picture:', error);
        this.errorMessage = error.error?.message || 'Failed to upload profile picture';
        this.toastService.error('Error', this.errorMessage);
        this.isSaving = false;
      }
    });
  }

  removeProfilePicture(): void {
    if (confirm('Are you sure you want to remove your profile picture?')) {
      if (!this.currentUser?.id) return;

      this.usersService.apiUsersIdProfilePictureDelete(this.currentUser.id).subscribe({
        next: () => {
          this.previewUrl = null;
          this.selectedFile = null;
          this.currentUser.profilePictureUrl = null;
          this.successMessage = 'Profile picture removed';
          this.toastService.success('Success', 'Profile picture removed successfully');
        },
        error: (error) => {
          console.error('Error removing profile picture:', error);
          this.errorMessage = error.error?.message || 'Failed to remove profile picture';
          this.toastService.error('Error', this.errorMessage);
        }
      });
    }
  }

  // Validators
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

  // Helper methods
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  private clearMessages(): void {
    this.successMessage = '';
    this.errorMessage = '';
  }

  // Getters for form controls
  get userName(): FormControl {
    return this.profileForm.get('userName') as FormControl;
  }

  get email(): FormControl {
    return this.profileForm.get('email') as FormControl;
  }

  get phoneNumber(): FormControl {
    return this.profileForm.get('phoneNumber') as FormControl;
  }

  get currentPassword(): FormControl {
    return this.passwordForm.get('currentPassword') as FormControl;
  }

  get newPassword(): FormControl {
    return this.passwordForm.get('newPassword') as FormControl;
  }

  get confirmPassword(): FormControl {
    return this.passwordForm.get('confirmPassword') as FormControl;
  }


  // Password validation helper methods
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

  // User initials for avatar
  getUserInitials(): string {
    if (this.currentUser?.name) {
      const parts = this.currentUser.name.split(' ');
      if (parts.length >= 2) {
        return (parts[0][0] + parts[1][0]).toUpperCase();
      }
      return this.currentUser.name.substring(0, 2).toUpperCase();
    }
    if (this.currentUser?.email) {
      return this.currentUser.email.substring(0, 2).toUpperCase();
    }
    return 'U';
  }

  // Navigation
  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
