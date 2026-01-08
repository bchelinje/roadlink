import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { UsersService } from '@core/api';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './verify-email.component.html',
  styleUrls: ['./verify-email.component.scss']
})
export class VerifyEmailComponent implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  isVerifying = true;
  verificationSuccess = false;
  errorMessage = '';

  userId = '';
  token = '';

  ngOnInit(): void {
    // Get userId and token from query params
    this.route.queryParams.subscribe(params => {
      this.userId = params['userId'] || '';
      this.token = params['token'] || '';

      if (!this.userId || !this.token) {
        this.isVerifying = false;
        this.errorMessage = 'Invalid verification link.';
        return;
      }

      this.verifyEmail();
    });
  }

  private verifyEmail(): void {
    this.usersService.apiUsersConfirmEmailGet(this.userId, this.token)
      .subscribe({
        next: (response: any) => {
          this.isVerifying = false;
          this.verificationSuccess = true;

          // Auto-redirect to login after 5 seconds
          setTimeout(() => {
            this.goToLogin();
          }, 5000);
        },
        error: (error) => {
          console.error('Email verification error:', error);
          this.isVerifying = false;
          this.verificationSuccess = false;
          this.errorMessage = error.error?.message || 'Email verification failed. The link may have expired.';
        }
      });
  }

  resendVerificationEmail(): void {
    // You'll need to implement this endpoint
    // For now, redirect to a page where they can request a new link
    this.router.navigate(['/resend-verification']);
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
