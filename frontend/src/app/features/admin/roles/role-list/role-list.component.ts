import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { RolesService, RoleDto } from '@core/api';
import {HeaderComponent} from '@app/layout/header/header.component';

@Component({
  selector: 'app-role-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './role-list.component.html',
  styleUrls: ['./role-list.component.scss']
})
export class RoleListComponent implements OnInit {
  private rolesService = inject(RolesService);
  private router = inject(Router);

  roles: RoleDto[] = [];
  filteredRoles: RoleDto[] = [];
  isLoading = false;
  errorMessage = '';
  searchTerm = '';
  deleteConfirmId: string | null = null;

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.rolesService.apiRolesGet().subscribe({
      next: (roles) => {
        this.roles = roles;
        this.filteredRoles = roles;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading roles:', error);
        this.errorMessage = 'Failed to load roles. Please try again.';
        this.isLoading = false;
      }
    });
  }

  onSearch(): void {
    if (!this.searchTerm.trim()) {
      this.filteredRoles = this.roles;
      return;
    }

    const search = this.searchTerm.toLowerCase();
    this.filteredRoles = this.roles.filter(role =>
      role.name?.toLowerCase().includes(search) ||
      role.description?.toLowerCase().includes(search)
    );
  }

  viewRole(roleId: string | undefined): void {
    if (roleId) {
      this.router.navigate(['/roles', roleId]);
    }
  }

  editRole(roleId: string | undefined): void {
    if (roleId) {
      this.router.navigate(['/roles', roleId, 'edit']);
    }
  }

  confirmDelete(roleId: string | undefined): void {
    this.deleteConfirmId = roleId || null;
  }

  cancelDelete(): void {
    this.deleteConfirmId = null;
  }

  deleteRole(roleId: string | undefined): void {
    if (!roleId) return;

    this.rolesService.apiRolesIdDelete(roleId).subscribe({
      next: () => {
        this.roles = this.roles.filter(r => r.id !== roleId);
        this.filteredRoles = this.filteredRoles.filter(r => r.id !== roleId);
        this.deleteConfirmId = null;
      },
      error: (error) => {
        console.error('Error deleting role:', error);
        this.errorMessage = error.error?.message || 'Failed to delete role. It may be assigned to users.';
        this.deleteConfirmId = null;
      }
    });
  }

  createNewRole(): void {
    this.router.navigate(['/roles/create']);
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
}
