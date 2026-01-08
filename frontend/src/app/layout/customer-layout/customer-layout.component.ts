import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-customer-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './customer-layout.component.html',
  styleUrls: ['./customer-layout.component.scss']
})
export class CustomerLayoutComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  currentUser$ = this.authService.currentUser$;
  isSidebarOpen = false;

  menuItems = [
    { path: '/customer/dashboard', icon: 'home', label: 'Dashboard' },
    { path: '/customer/my-jobs', icon: 'briefcase', label: 'My Jobs' },
    { path: '/customer/request-job', icon: 'plus', label: 'Request Service' },
    { path: '/customer/book-job', icon: 'credit-card', label: 'Book & Pay' },
    { path: '/customer/job-templates', icon: 'template', label: 'Job Templates' },
    { path: '/customer/recurring-jobs', icon: 'refresh', label: 'Recurring Jobs' },
    { path: '/customer/addresses', icon: 'map-pin', label: 'My Addresses' },
    { path: '/customer/favorites', icon: 'star', label: 'Favorite Drivers' },
    { path: '/customer/payments', icon: 'dollar-sign', label: 'Payment History' },
    { path: '/customer/my-reviews', icon: 'message-square', label: 'My Reviews' },
    { path: '/customer/price-calculator', icon: 'calculator', label: 'Price Calculator' },
    { path: '/customer/profile', icon: 'user', label: 'Profile' },
    { path: '/customer/gdpr-privacy', icon: 'shield', label: 'Privacy & Data' }
  ];

  getUserInitials(email: string): string {
    if (!email) return 'U';
    return email.charAt(0).toUpperCase();
  }

  toggleSidebar(): void {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
