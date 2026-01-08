// layouts/main-layout/main-layout.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { filter } from 'rxjs/operators';
import { NotificationsBellComponent } from '@app/shared/components/notifications/notifications-bell.component';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  badge?: number;
  children?: NavItem[];
}

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, NotificationsBellComponent],
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss']
})
export class MainLayoutComponent implements OnInit {
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
      route: '/dashboard'
    },
    {
      label: 'User Management',
      icon: 'users',
      route: '/users',
      children: [
        {
          label: 'All Users',
          icon: 'list',
          route: '/users'
        },
        {
          label: 'Create User',
          icon: 'user-plus',
          route: '/users/create'
        }
      ]
    },
    {
      label: 'Drivers',
      icon: 'truck',
      route: '/drivers',
      children: [
        {
          label: 'All Drivers',
          icon: 'list',
          route: '/drivers'
        },
        {
          label: 'Create Driver',
          icon: 'user-plus',
          route: '/drivers/create'
        },
        {
          label: 'Active Drivers',
          icon: 'map',
          route: '/active-drivers'
        }
      ]
    },
    {
      label: 'Job Management',
      icon: 'briefcase',
      route: '/jobs',
      children: [
        {
          label: 'All Jobs',
          icon: 'list',
          route: '/jobs'
        },
        {
          label: 'Create Job',
          icon: 'plus',
          route: '/jobs/create'
        },
        {
          label: 'Bulk Create',
          icon: 'copy',
          route: '/jobs/bulk-create'
        }
      ]
    },
    {
      label: 'Documents',
      icon: 'file-text',
      route: '/documents'
    },
    {
      label: 'Pricing Rules',
      icon: 'tag',
      route: '/pricing-rules'
    },
    {
      label: 'Roles & Permissions',
      icon: 'shield',
      route: '/roles'
    },
    {
      label: 'GDPR & Privacy',
      icon: 'lock',
      route: '/gdpr-management',
      children: [
        {
          label: 'Deletion Requests',
          icon: 'trash',
          route: '/gdpr-management'
        },
        {
          label: 'User Data Management',
          icon: 'database',
          route: '/user-data-management'
        }
      ]
    },
    {
      label: 'Activity Logs',
      icon: 'activity',
      route: '/activity-logs',
      children: [
        {
          label: 'Standard Logs',
          icon: 'list',
          route: '/activity-logs'
        },
        {
          label: 'Advanced Analytics',
          icon: 'bar-chart',
          route: '/activity-logs-advanced'
        }
      ]
    },
    {
      label: 'My Profile',
      icon: 'user',
      route: '/profile'
    },
    {
      label: 'Notifications',
      icon: 'bell',
      route: '/notifications/preferences'
    },
    {
      label: 'Settings',
      icon: 'settings',
      route: '/settings'
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

    if (url.includes('/dashboard')) this.pageTitle = 'Dashboard';
    else if (url.includes('/users/create')) this.pageTitle = 'Create User';
    else if (url.includes('/users') && url.includes('/edit')) this.pageTitle = 'Edit User';
    else if (url.includes('/users/')) this.pageTitle = 'User Details';
    else if (url.includes('/users')) this.pageTitle = 'Users';
    else if (url.includes('/drivers') && url.includes('/edit')) this.pageTitle = 'Edit Driver';
    else if (url.includes('/drivers/')) this.pageTitle = 'Driver Details';
    else if (url.includes('/drivers')) this.pageTitle = 'Drivers';
    else if (url.includes('/roles')) this.pageTitle = 'Roles';
    else if (url.includes('/gdpr-management')) this.pageTitle = 'GDPR Deletion Requests';
    else if (url.includes('/user-data-management')) this.pageTitle = 'User Data Management';
    else if (url.includes('/activity-logs')) this.pageTitle = 'Activity Logs';
    else if (url.includes('/profile')) this.pageTitle = 'My Profile';
    else if (url.includes('/settings')) this.pageTitle = 'Settings';
    else this.pageTitle = 'LoadLink';
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
    return 'U';
  }

  getIcon(iconName: string): string {
    const icons: Record<string, string> = {
      'home': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />',
      'users': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />',
      'list': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />',
      'user-plus': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z" />',
      'truck': '<path d="M4 16c0 .829.335 1.577.879 2.121M4 16c0-.829.335-1.577.879-2.121M4 16h12M20 16c0 .829-.335 1.577-.879 2.121M20 16c0-.829-.335-1.577-.879-2.121M20 16H8m-3.121 2.121C5.333 18.667 6.127 19 7 19c.873 0 1.667-.333 2.121-.879M4.879 13.879C5.333 13.333 6.127 13 7 13c.873 0 1.667.333 2.121.879M9.121 18.121C9.667 17.667 10 16.873 10 16c0-.873-.333-1.667-.879-2.121M9.121 13.879C9.667 14.333 10 15.127 10 16m7 4c0-.373-.075-.73-.207-1.063M17 20c.873 0 1.667-.333 2.121-.879M17 20c-.873 0-1.667-.333-2.121-.879M17 13c.873 0 1.667.333 2.121.879M17 13c-.873 0-1.667.333-2.121.879M19.121 13.879C19.667 14.333 20 15.127 20 16M1 6h9l3 6H3L1 6zm20 6h-3V3H8v3" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" />',
      'briefcase': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />',
      'shield': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />',
      'activity': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />',
      'user': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />',
      'settings': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />',
      'plus': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />',
      'copy': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />',
      'map': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />',
      'file-text': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />',
      'tag': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />',
      'bell': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />',
      'bar-chart': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />',
      'lock': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />',
      'trash': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />',
      'database': '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4" />'
    };
    return icons[iconName] || icons['home'];
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
