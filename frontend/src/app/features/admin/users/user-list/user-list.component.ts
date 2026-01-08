import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { UsersService, UserViewModel } from '@core/api';
import { UserActionsComponent } from '@shared/components/user-actions/user-actions.component';
import {HeaderComponent} from '@app/layout/header/header.component';
import {LayoutModule} from '@angular/cdk/layout';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, UserActionsComponent, LayoutModule],
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss']
})
export class UserListComponent implements OnInit {
  private usersService = inject(UsersService);
  private toastService = inject(ToastService);

  users: UserViewModel[] = [];
  filteredUsers: UserViewModel[] = [];
  isLoading = false;
  errorMessage = '';

  // 👇 ADD THIS
  now = new Date();

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalPages = 0;
  pageSizes = [5, 10, 25, 50];

  // Search
  searchControl = new FormControl('');

  // Sort
  sortColumn: keyof UserViewModel = 'email';
  sortDirection: 'asc' | 'desc' = 'asc';

  // Selection
  selectedUsers = new Set<string>();
  selectAll = false;

  ngOnInit(): void {
    this.loadUsers();
    this.setupSearch();
  }

  setupSearch(): void {
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(searchTerm => {
      this.filterUsers(searchTerm || '');
    });
  }

  loadUsers(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.usersService.apiUsersGet().subscribe({
      next: (response) => {
        this.users = response.users || [];
        this.filteredUsers = response.users || [];
        this.calculatePagination();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading users:', error);
        this.errorMessage = 'Failed to load users. Please try again.';
        this.isLoading = false;
      }
    });
  }

  filterUsers(searchTerm: string): void {
    if (!searchTerm) {
      this.filteredUsers = [...this.users];
    } else {
      const term = searchTerm.toLowerCase();
      this.filteredUsers = this.users.filter(user =>
        user.email?.toLowerCase().includes(term) ||
        user.userName?.toLowerCase().includes(term) ||
        user.phoneNumber?.toLowerCase().includes(term)
      );
    }
    this.currentPage = 1;
    this.calculatePagination();
  }

  sortBy(column: keyof UserViewModel): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }

    this.filteredUsers.sort((a, b) => {
      const aValue = a[column] as any;
      const bValue = b[column] as any;

      if (aValue === bValue) return 0;

      const comparison = aValue > bValue ? 1 : -1;
      return this.sortDirection === 'asc' ? comparison : -comparison;
    });
  }

  calculatePagination(): void {
    this.totalPages = Math.ceil(this.filteredUsers.length / this.pageSize);
    if (this.currentPage > this.totalPages) {
      this.currentPage = Math.max(1, this.totalPages);
    }
  }

  get paginatedUsers(): UserViewModel[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    return this.filteredUsers.slice(startIndex, endIndex);
  }

  get paginationInfo(): string {
    const start = (this.currentPage - 1) * this.pageSize + 1;
    const end = Math.min(this.currentPage * this.pageSize, this.filteredUsers.length);
    return `Showing ${start} to ${end} of ${this.filteredUsers.length} users`;
  }

  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  changePageSize(size: number): void {
    this.pageSize = size;
    this.currentPage = 1;
    this.calculatePagination();
  }

  toggleSelectAll(): void {
    this.selectAll = !this.selectAll;
    this.selectedUsers.clear();

    if (this.selectAll) {
      this.paginatedUsers.forEach(user => {
        if (user.id) {
          this.selectedUsers.add(user.id);
        }
      });
    }
  }

  toggleUserSelection(userId: string): void {
    if (this.selectedUsers.has(userId)) {
      this.selectedUsers.delete(userId);
    } else {
      this.selectedUsers.add(userId);
    }

    this.selectAll = this.selectedUsers.size === this.paginatedUsers.length;
  }

  isUserSelected(userId: string): boolean {
    return this.selectedUsers.has(userId);
  }

  deleteUser(userId: string): void {
    if (confirm('Are you sure you want to delete this user?')) {
      this.usersService.apiUsersIdDelete(userId).subscribe({
        next: () => {
          this.loadUsers();
          this.selectedUsers.delete(userId);
          this.toastService.success('Success', 'User deleted successfully!');
        },
        error: (error) => {
          console.error('Error deleting user:', error);
          const errorMessage = error.error?.message || error.error?.title || 'Failed to delete user. Please try again.';
          this.toastService.error('Error Deleting User', errorMessage);
        }
      });
    }
  }

  deleteSelectedUsers(): void {
    if (this.selectedUsers.size === 0) {
      this.toastService.warning('No Selection', 'Please select users to delete.');
      return;
    }

    if (confirm(`Are you sure you want to delete ${this.selectedUsers.size} user(s)?`)) {
      const deletePromises = Array.from(this.selectedUsers).map(userId =>
        this.usersService.apiUsersIdDelete(userId).toPromise()
      );

      Promise.all(deletePromises).then(
        () => {
          this.loadUsers();
          this.selectedUsers.clear();
          this.selectAll = false;
          this.toastService.success('Success', `${this.selectedUsers.size} user(s) deleted successfully!`);
        },
        (error) => {
          console.error('Error deleting users:', error);
          const errorMessage = 'Failed to delete some users. Please try again.';
          this.toastService.error('Error Deleting Users', errorMessage);
        }
      );
    }
  }

  getRoleNames(user: UserViewModel): string {
    return user.roles?.join(', ') || 'No roles';
  }

  getUserStatusBadge(user: UserViewModel): string {
    if (user.lockoutEnd && new Date(user.lockoutEnd) > new Date()) {
      return 'locked';
    }
    if (!user.emailConfirmed) {
      return 'unconfirmed';
    }
    return 'active';
  }

  getUserStatusClass(user: UserViewModel): string {
    const status = this.getUserStatusBadge(user);
    return {
      'active': 'bg-green-100 text-green-800',
      'locked': 'bg-red-100 text-red-800',
      'unconfirmed': 'bg-yellow-100 text-yellow-800'
    }[status] || '';
  }

  getUserStatusText(user: UserViewModel): string {
    const status = this.getUserStatusBadge(user);
    return {
      'active': 'Active',
      'locked': 'Locked',
      'unconfirmed': 'Unconfirmed'
    }[status] || '';
  }

  exportToCSV(): void {
    const headers = ['Email', 'Username', 'Phone', 'Roles', 'Status'];
    const rows = this.filteredUsers.map(user => [
      user.email || '',
      user.userName || '',
      user.phoneNumber || '',
      this.getRoleNames(user),
      this.getUserStatusText(user)
    ]);

    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.map(cell => `"${cell}"`).join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `users-${new Date().toISOString().split('T')[0]}.csv`;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  // 👇 ADD THESE NEW HELPER METHODS
  /**
   * Check if user is currently locked
   */
  isUserLocked(user: UserViewModel): boolean {
    if (!user.lockoutEnabled || !user.lockoutEnd) {
      return false;
    }
    const lockoutDate = new Date(user.lockoutEnd);
    return lockoutDate > this.now;
  }

  /**
   * Get user lockout end date or null
   */
  getUserLockoutEnd(user: UserViewModel): Date | null {
    if (!user.lockoutEnd) {
      return null;
    }
    return new Date(user.lockoutEnd);
  }
}
