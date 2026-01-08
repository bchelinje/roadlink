import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { RolesService, UsersService, RoleDetailDto, UserViewModel } from '@core/api';

@Component({
  selector: 'app-role-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './role-detail.component.html',
  styleUrls: ['./role-detail.component.scss']
})
export class RoleDetailComponent implements OnInit {
  private rolesService = inject(RolesService);
  private usersService = inject(UsersService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  roleId: string = '';
  role: RoleDetailDto | null = null;
  usersWithRole: UserViewModel[] = [];
  isLoading = false;
  isLoadingUsers = false;
  errorMessage = '';
  deleteConfirm = false;

  ngOnInit(): void {
    this.roleId = this.route.snapshot.paramMap.get('id') || '';
    if (this.roleId) {
      this.loadRole();
      this.loadUsersWithRole();
    }
  }

  loadRole(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.rolesService.apiRolesIdGet(this.roleId).subscribe({
      next: (role) => {
        this.role = role;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading role:', error);
        this.errorMessage = 'Failed to load role details.';
        this.isLoading = false;
      }
    });
  }

  loadUsersWithRole(): void {
    this.isLoadingUsers = true;

    // Get all users and filter by role
    this.usersService.apiUsersGet().subscribe({
      next: (response) => {
        // Filter users who have this role
        this.usersWithRole = (response.users || []).filter(user =>
          user.roles?.some(roleName =>
            roleName?.toLowerCase() === this.role?.name?.toLowerCase()
          )
        );
        this.isLoadingUsers = false;
      },
      error: (error) => {
        console.error('Error loading users:', error);
        this.isLoadingUsers = false;
      }
    });
  }

  editRole(): void {
    this.router.navigate(['/roles', this.roleId, 'edit']);
  }

  confirmDelete(): void {
    this.deleteConfirm = true;
  }

  cancelDelete(): void {
    this.deleteConfirm = false;
  }

  deleteRole(): void {
    this.rolesService.apiRolesIdDelete(this.roleId).subscribe({
      next: () => {
        this.router.navigate(['/roles']);
      },
      error: (error) => {
        console.error('Error deleting role:', error);
        this.errorMessage = error.error?.message || 'Failed to delete role. It may be assigned to users.';
        this.deleteConfirm = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/roles']);
  }

  viewUser(userId: string | undefined): void {
    if (userId) {
      this.router.navigate(['/users', userId]);
    }
  }

  getRoleIcon(roleName: string | null | undefined): string {
    const name = roleName?.toLowerCase() || '';

    if (name.includes('admin') || name.includes('super')) {
      return '👑';
    } else if (name.includes('manager') || name.includes('lead')) {
      return '👔';
    } else if (name.includes('user') || name.includes('member')) {
      return '👤';
    } else if (name.includes('guest') || name.includes('viewer')) {
      return '👁️';
    }

    return '🎭';
  }

  getUserInitials(user: UserViewModel): string {
    if (user.userName) {
      return user.userName.substring(0, 2).toUpperCase();
    }
    if (user.email) {
      return user.email.substring(0, 2).toUpperCase();
    }
    return 'U';
  }
}
