import { Component, inject, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UsersService } from '@core/api';

@Component({
  selector: 'app-user-actions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-actions.component.html',
  styleUrls: ['./user-actions.component.scss']
})
export class UserActionsComponent {
  private readonly usersService = inject(UsersService);

  @Input() userId!: string;
  @Input() userEmail!: string;
  @Input() isLocked: boolean = false;
  @Input() lockoutEnd: Date | null = null;
  @Output() actionCompleted = new EventEmitter<void>();

  showLockModal = false;
  showUnlockModal = false;
  showResendVerificationModal = false;

  lockDuration = 7; // Default to 7 days
  customDuration = false;
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  openLockModal(): void {
    this.showLockModal = true;
    this.errorMessage = '';
    this.successMessage = '';
  }

  openUnlockModal(): void {
    this.showUnlockModal = true;
    this.errorMessage = '';
    this.successMessage = '';
  }

  openResendVerificationModal(): void {
    this.showResendVerificationModal = true;
    this.errorMessage = '';
    this.successMessage = '';
  }

  closeLockModal(): void {
    this.showLockModal = false;
    this.lockDuration = 7;
    this.customDuration = false;
  }

  closeUnlockModal(): void {
    this.showUnlockModal = false;
  }

  closeResendVerificationModal(): void {
    this.showResendVerificationModal = false;
  }

  lockUser(): void {
    this.isLoading = true;
    this.errorMessage = '';

    // Convert days to minutes (1 day = 1440 minutes)
    const durationInMinutes = this.customDuration ? null : this.lockDuration * 1440;

    this.usersService.apiUsersIdLockPost(this.userId, {
      lockoutDurationMinutes: durationInMinutes
    }).subscribe({
      next: (response: any) => {
        this.successMessage = 'User locked successfully';
        this.isLoading = false;
        setTimeout(() => {
          this.closeLockModal();
          this.actionCompleted.emit();
        }, 1500);
      },
      error: (error) => {
        console.error('Lock user error:', error);
        this.errorMessage = error.error?.message || 'Failed to lock user';
        this.isLoading = false;
      }
    });
  }

  unlockUser(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.usersService.apiUsersIdUnlockPost(this.userId).subscribe({
      next: (response: any) => {
        this.successMessage = 'User unlocked successfully';
        this.isLoading = false;
        setTimeout(() => {
          this.closeUnlockModal();
          this.actionCompleted.emit();
        }, 1500);
      },
      error: (error) => {
        console.error('Unlock user error:', error);
        this.errorMessage = error.error?.message || 'Failed to unlock user';
        this.isLoading = false;
      }
    });
  }

  resendVerification(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.usersService.apiUsersResendConfirmationPost({
      email: this.userEmail
    }).subscribe({
      next: (response: any) => {
        this.successMessage = 'Verification email sent successfully';
        this.isLoading = false;
        setTimeout(() => {
          this.closeResendVerificationModal();
          this.actionCompleted.emit();
        }, 1500);
      },
      error: (error) => {
        console.error('Resend verification error:', error);
        this.errorMessage = error.error?.message || 'Failed to send verification email';
        this.isLoading = false;
      }
    });
  }

  getLockoutStatusText(): string {
    if (!this.isLocked || !this.lockoutEnd) {
      return 'Active';
    }

    const now = new Date();
    const lockoutDate = new Date(this.lockoutEnd);

    if (lockoutDate > now) {
      const daysLeft = Math.ceil((lockoutDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
      return `Locked (${daysLeft} days remaining)`;
    }

    return 'Active';
  }
}
