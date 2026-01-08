import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { UsersService, UserViewModel } from '@core/api';
import {HeaderComponent} from '@app/layout/header/header.component';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent],
  templateUrl: './user-detail.component.html',
  styleUrls: ['./user-detail.component.scss']
})
export class UserDetailComponent implements OnInit {
  private usersService = inject(UsersService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private toastService = inject(ToastService);

  userId: string = '';
  user: UserViewModel | null = null;
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.userId = this.route.snapshot.paramMap.get('id') || '';
    if (this.userId) {
      this.loadUser();
    }
  }

  loadUser(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.usersService.apiUsersIdGet(this.userId).subscribe({
      next: (user) => {
        this.user = user;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading user:', error);
        this.errorMessage = 'Failed to load user. Please try again.';
        this.isLoading = false;
      }
    });
  }

  deleteUser(): void {
    if (confirm('Are you sure you want to delete this user? This action cannot be undone.')) {
      this.usersService.apiUsersIdDelete(this.userId).subscribe({
        next: () => {
          this.router.navigate(['/users']);
        },
        error: (error) => {
          console.error('Error deleting user:', error);
          alert('Failed to delete user. Please try again.');
        }
      });
    }
  }

  unlockAccount(): void {
    if (confirm('Unlock this user account?')) {
      this.usersService.apiUsersIdUnlockPost(this.userId).subscribe({
        next: () => {
          this.toastService.success('Success', 'Account unlocked successfully');
          this.loadUser(); // Reload to show updated status
        },
        error: (err: any) => {
          console.error('Failed to unlock account:', err);
          this.toastService.error('Error', 'Failed to unlock account');
        }
      });
    }
  }

  resetPassword(): void {
    if (confirm('Send password reset email to this user?')) {
      if (!this.user?.email) return;

      this.usersService.apiUsersForgotPasswordPost({ email: this.user.email }).subscribe({
        next: () => {
          this.toastService.success('Success', 'Password reset email sent successfully');
        },
        error: (err: any) => {
          console.error('Failed to send password reset email:', err);
          this.toastService.error('Error', 'Failed to send password reset email');
        }
      });
    }
  }

  /**
   * Check if account is currently locked
   */
  isAccountLocked(): boolean {
    if (!this.user || !this.user.lockoutEnd) {
      return false;
    }
    return new Date(this.user.lockoutEnd) > new Date();
  }

  getUserStatusBadge(): string {
    if (!this.user) return 'unknown';
    if (this.isAccountLocked()) {
      return 'locked';
    }
    if (!this.user.emailConfirmed) {
      return 'unconfirmed';
    }
    return 'active';
  }

  getUserStatusClass(): string {
    const status = this.getUserStatusBadge();
    return {
      'active': 'bg-green-100 text-green-800',
      'locked': 'bg-red-100 text-red-800',
      'unconfirmed': 'bg-yellow-100 text-yellow-800',
      'unknown': 'bg-gray-100 text-gray-800'
    }[status] || '';
  }

  getUserStatusText(): string {
    const status = this.getUserStatusBadge();
    return {
      'active': 'Active',
      'locked': 'Locked',
      'unconfirmed': 'Unconfirmed',
      'unknown': 'Unknown'
    }[status] || '';
  }

  formatDate(date: string | undefined | null): string {
    if (!date) return 'Never';
    return new Date(date).toLocaleString();
  }
}
