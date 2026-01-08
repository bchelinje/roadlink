import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { Subject, takeUntil, forkJoin } from 'rxjs';
import { AuthService } from '@core/services/auth.service';
import { UsersService, RolesService, UserViewModel, RoleDetailDto } from '@core/api';

interface Move {
  id: string;
  customer: string;
  from: string;
  to: string;
  date: string;
  status: 'pending' | 'in-progress' | 'completed' | 'cancelled';
  revenue: number;
}

interface Driver {
  id: string;
  name: string;
  status: 'available' | 'on-move' | 'off-duty';
  movesToday: number;
  rating: number;
}

interface DashboardStats {
  totalUsers: number;
  activeUsers: number;
  totalRoles: number;
  newUsersThisWeek: number;
  usersGrowth: number;
  activeUsersGrowth: number;
}

interface RoleDistribution {
  roleName: string;
  userCount: number;
  percentage: number;
  color: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: []
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly usersService = inject(UsersService);
  private readonly rolesService = inject(RolesService);
  private readonly router = inject(Router);
  private destroy$ = new Subject<void>();

  userEmail = '';
  userName = '';
  isLoading = true;

  stats: DashboardStats = {
    totalUsers: 0,
    activeUsers: 0,
    totalRoles: 0,
    newUsersThisWeek: 0,
    usersGrowth: 0,
    activeUsersGrowth: 0
  };

  recentUsers: UserViewModel[] = [];
  roleDistribution: RoleDistribution[] = [];
  allUsers: UserViewModel[] = [];
  allRoles: RoleDetailDto[] = [];

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadDashboardData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadUserInfo(): void {
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        if (user) {
          this.userEmail = user.email;
          this.userName = user.name || user.email.split('@')[0];
        }
      });
  }

  private loadDashboardData(): void {
    this.isLoading = true;

    // Fetch users and roles in parallel
    forkJoin({
      users: this.usersService.apiUsersGet(),
      roles: this.rolesService.apiRolesGet()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ users, roles }) => {
          this.allUsers = users.users || [];
          this.allRoles = roles;

          this.calculateStats();
          this.calculateRoleDistribution();
          this.getRecentUsers();

          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading dashboard data:', error);
          this.isLoading = false;
        }
      });
  }

  private calculateStats(): void {
    // Total users
    this.stats.totalUsers = this.allUsers.length;

    // Active users (email confirmed)
    this.stats.activeUsers = this.allUsers.filter(u => u.emailConfirmed).length;

    // Total roles
    this.stats.totalRoles = this.allRoles.length;

    // New users this week (mock for now - would need creation date from API)
    this.stats.newUsersThisWeek = Math.floor(this.stats.totalUsers * 0.05); // 5% as example

    // Calculate growth percentages (mock - would need historical data)
    this.stats.usersGrowth = 12;
    this.stats.activeUsersGrowth = 8;
  }

  private calculateRoleDistribution(): void {
    const roleColors = [
      '#3B82F6', // blue
      '#F97316', // orange
      '#10B981', // green
      '#8B5CF6', // purple
      '#EC4899', // pink
      '#F59E0B', // amber
      '#06B6D4', // cyan
      '#EF4444'  // red
    ];

    // Count users per role
    const roleCounts = new Map<string, number>();

    this.allUsers.forEach(user => {
      if (user.roles && user.roles.length > 0) {
        user.roles.forEach(role => {
          const count = roleCounts.get(role!) || 0;
          roleCounts.set(role!, count + 1);
        });
      }
    });

    // Convert to array and calculate percentages
    this.roleDistribution = Array.from(roleCounts.entries())
      .map(([roleName, count], index) => ({
        roleName,
        userCount: count,
        percentage: Math.round((count / this.stats.totalUsers) * 100),
        color: roleColors[index % roleColors.length]
      }))
      .sort((a, b) => b.userCount - a.userCount);
  }

  private getRecentUsers(): void {
    // Get last 5 users (in real app, would sort by creation date)
    this.recentUsers = this.allUsers.slice(-5).reverse();
  }

  getUserInitials(user: UserViewModel): string {
    if (user.userName) {
      const parts = user.userName.split(' ');
      if (parts.length >= 2) {
        return (parts[0][0] + parts[1][0]).toUpperCase();
      }
      return user.userName.substring(0, 2).toUpperCase();
    }
    if (user.email) {
      return user.email.substring(0, 2).toUpperCase();
    }
    return 'U';
  }

  getUserRoleBadgeColor(user: UserViewModel): string {
    if (!user.roles || user.roles.length === 0) return 'bg-gray-100 text-gray-800';

    const role = user.roles[0]?.toLowerCase() || '';

    if (role.includes('super') || role.includes('admin')) {
      return 'bg-red-100 text-red-800';
    } else if (role.includes('manager') || role.includes('lead')) {
      return 'bg-blue-100 text-blue-800';
    } else if (role.includes('driver')) {
      return 'bg-green-100 text-green-800';
    } else if (role.includes('dispatcher')) {
      return 'bg-purple-100 text-purple-800';
    }

    return 'bg-gray-100 text-gray-800';
  }

  viewUser(userId: string): void {
    this.router.navigate(['/users', userId]);
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }

  refreshDashboard(): void {
    this.loadDashboardData();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
