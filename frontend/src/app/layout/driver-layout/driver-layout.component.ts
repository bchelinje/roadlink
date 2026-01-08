// layouts/driver-layout/driver-layout.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { filter } from 'rxjs/operators';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  badge?: number;
  children?: NavItem[];
}

@Component({
  selector: 'app-driver-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './driver-layout.component.html',
  styleUrls: ['./driver-layout.component.scss']
})
export class DriverLayoutComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  sidebarOpen = false;
  expandedMenus = new Set<string>();
  userName = '';
  userEmail = '';
  pageTitle = 'Dashboard';

  navItems: NavItem[] = [
    {
      label: 'Dashboard',
      icon: 'home',
      route: '/driver/dashboard'
    },
    {
      label: 'Marketplace',
      icon: 'shopping-bag',
      route: '/driver/marketplace'
    },
    {
      label: 'My Jobs',
      icon: 'clipboard',
      route: '/driver/jobs'
    },
    {
      label: 'Schedule',
      icon: 'calendar',
      route: '/driver/schedule'
    },
    {
      label: 'My Vehicles',
      icon: 'truck',
      route: '/driver/vehicles'
    },
    {
      label: 'My Earnings',
      icon: 'dollar',
      route: '/driver/earnings'
    },
    {
      label: 'My Documents',
      icon: 'file',
      route: '/driver/documents'
    },
    {
      label: 'My Reviews',
      icon: 'star',
      route: '/driver/reviews'
    },
    {
      label: 'My Profile',
      icon: 'user',
      route: '/driver/profile'
    }
  ];

  ngOnInit(): void {
    this.loadUserInfo();
    this.updatePageTitle();

    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.updatePageTitle();
        this.sidebarOpen = false; // Close sidebar on navigation (mobile)
      });
  }

  private loadUserInfo(): void {
    this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.userEmail = user.email;
        this.userName = user.name || user.email.split('@')[0];
      }
    });
  }

  private updatePageTitle(): void {
    const url = this.router.url;

    if (url.includes('/driver/dashboard')) this.pageTitle = 'Dashboard';
    else if (url.includes('/driver/marketplace')) this.pageTitle = 'Job Marketplace';
    else if (url.includes('/driver/jobs') && url.split('/').length > 3) this.pageTitle = 'Job Details';
    else if (url.includes('/driver/jobs')) this.pageTitle = 'My Jobs';
    else if (url.includes('/driver/schedule')) this.pageTitle = 'Schedule';
    else if (url.includes('/driver/vehicles')) this.pageTitle = 'My Vehicles';
    else if (url.includes('/driver/earnings')) this.pageTitle = 'My Earnings';
    else if (url.includes('/driver/documents')) this.pageTitle = 'My Documents';
    else if (url.includes('/driver/reviews')) this.pageTitle = 'My Reviews';
    else if (url.includes('/driver/profile')) this.pageTitle = 'My Profile';
    else this.pageTitle = 'Driver Portal';
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }

  toggleSubmenu(label: string): void {
    if (this.expandedMenus.has(label)) {
      this.expandedMenus.delete(label);
    } else {
      this.expandedMenus.add(label);
    }
  }

  getUserInitials(): string {
    if (this.userName) {
      const parts = this.userName.split(' ');
      if (parts.length >= 2) {
        return (parts[0][0] + parts[1][0]).toUpperCase();
      }
      return this.userName.substring(0, 2).toUpperCase();
    }
    return 'D';
  }

  getIcon(iconName: string): string {
    const icons: Record<string, string> = {
      'home': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />',
      'shopping-bag': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />',
      'clipboard': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />',
      'calendar': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />',
      'truck': '<path d="M4 16c0 .829.335 1.577.879 2.121M4 16c0-.829.335-1.577.879-2.121M4 16h12M20 16c0 .829-.335 1.577-.879 2.121M20 16c0-.829-.335-1.577-.879-2.121M20 16H8m-3.121 2.121C5.333 18.667 6.127 19 7 19c.873 0 1.667-.333 2.121-.879M4.879 13.879C5.333 13.333 6.127 13 7 13c.873 0 1.667.333 2.121.879M9.121 18.121C9.667 17.667 10 16.873 10 16c0-.873-.333-1.667-.879-2.121M9.121 13.879C9.667 14.333 10 15.127 10 16m7 4c0-.373-.075-.73-.207-1.063M17 20c.873 0 1.667-.333 2.121-.879M17 20c-.873 0-1.667-.333-2.121-.879M17 13c.873 0 1.667.333 2.121.879M17 13c-.873 0-1.667.333-2.121.879M19.121 13.879C19.667 14.333 20 15.127 20 16M1 6h9l3 6H3L1 6zm20 6h-3V3H8v3" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" />',
      'user': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />',
      'dollar': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />',
      'file': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />',
      'star': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />',
      'check-circle': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />'
    };
    return icons[iconName] || icons['home'];
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
