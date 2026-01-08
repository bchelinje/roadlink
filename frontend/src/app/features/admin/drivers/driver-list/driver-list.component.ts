import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { DriversService, DriverDto } from '@core/api';
import { HeaderComponent } from '@app/layout/header/header.component';
import { LayoutModule } from '@angular/cdk/layout';

@Component({
  selector: 'app-driver-list',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, HeaderComponent, LayoutModule],
  templateUrl: './driver-list.component.html',
  styleUrls: ['./driver-list.component.scss']
})
export class DriverListComponent implements OnInit {
  private driversService = inject(DriversService);

  drivers: DriverDto[] = [];
  filteredDrivers: DriverDto[] = [];
  isLoading = false;
  errorMessage = '';

  now = new Date();

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalPages = 0;
  pageSizes = [5, 10, 25, 50];

  // Search
  searchControl = new FormControl('');

  // Sort
  sortColumn: keyof DriverDto = 'lastName';
  sortDirection: 'asc' | 'desc' = 'asc';

  // Selection
  selectedDrivers = new Set<string>();
  selectAll = false;

  // Status filter
  statusFilter = new FormControl('all');
  statuses = ['all', 'Available', 'OnJob', 'Offline', 'Inactive'];

  ngOnInit(): void {
    this.loadDrivers();
    this.setupSearch();
    this.setupStatusFilter();
  }

  setupSearch(): void {
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(searchTerm => {
      this.filterDrivers();
    });
  }

  setupStatusFilter(): void {
    this.statusFilter.valueChanges.subscribe(() => {
      this.filterDrivers();
    });
  }

  loadDrivers(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const statusValue = this.statusFilter.value;
    const status = (statusValue && statusValue !== 'all') ? statusValue : undefined;

    this.driversService.apiDriversGet(status, undefined, undefined, undefined, undefined, undefined, 1, 1000).subscribe({
      next: (result) => {
        this.drivers = result.items || [];
        this.filterDrivers();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading drivers:', error);
        this.errorMessage = 'Failed to load drivers. Please try again.';
        this.isLoading = false;
      }
    });
  }

  filterDrivers(): void {
    const searchTerm = this.searchControl.value?.toLowerCase() || '';
    const status = this.statusFilter.value || 'all';

    this.filteredDrivers = this.drivers.filter(driver => {
      const matchesSearch = !searchTerm ||
        driver.firstName?.toLowerCase().includes(searchTerm) ||
        driver.lastName?.toLowerCase().includes(searchTerm) ||
        driver.email?.toLowerCase().includes(searchTerm) ||
        driver.phone?.toLowerCase().includes(searchTerm) ||
        driver.licenseNumber?.toLowerCase().includes(searchTerm);

      const matchesStatus = status === 'all' || driver.status === status;

      return matchesSearch && matchesStatus;
    });

    this.currentPage = 1;
    this.calculatePagination();
  }

  sortBy(column: keyof DriverDto): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }

    this.filteredDrivers.sort((a, b) => {
      const aValue = a[column] as any;
      const bValue = b[column] as any;

      if (aValue === bValue) return 0;

      const comparison = aValue > bValue ? 1 : -1;
      return this.sortDirection === 'asc' ? comparison : -comparison;
    });
  }

  calculatePagination(): void {
    this.totalPages = Math.ceil(this.filteredDrivers.length / this.pageSize);
    if (this.currentPage > this.totalPages) {
      this.currentPage = Math.max(1, this.totalPages);
    }
  }

  get paginatedDrivers(): DriverDto[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    return this.filteredDrivers.slice(startIndex, endIndex);
  }

  get paginationInfo(): string {
    const start = (this.currentPage - 1) * this.pageSize + 1;
    const end = Math.min(this.currentPage * this.pageSize, this.filteredDrivers.length);
    return `Showing ${start} to ${end} of ${this.filteredDrivers.length} drivers`;
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
    this.selectedDrivers.clear();

    if (this.selectAll) {
      this.paginatedDrivers.forEach(driver => {
        if (driver.id) {
          this.selectedDrivers.add(driver.id);
        }
      });
    }
  }

  toggleDriverSelection(driverId: string): void {
    if (this.selectedDrivers.has(driverId)) {
      this.selectedDrivers.delete(driverId);
    } else {
      this.selectedDrivers.add(driverId);
    }

    this.selectAll = this.selectedDrivers.size === this.paginatedDrivers.length;
  }

  isDriverSelected(driverId: string): boolean {
    return this.selectedDrivers.has(driverId);
  }

  getDriverFullName(driver: DriverDto): string {
    return `${driver.firstName || ''} ${driver.lastName || ''}`.trim() || 'N/A';
  }

  getStatusBadgeClass(status: string | null): string {
    const statusMap: { [key: string]: string } = {
      'Available': 'bg-green-100 text-green-800',
      'OnJob': 'bg-blue-100 text-blue-800',
      'Offline': 'bg-gray-100 text-gray-800',
      'Inactive': 'bg-red-100 text-red-800'
    };
    return status ? statusMap[status] || 'bg-gray-100 text-gray-800' : 'bg-gray-100 text-gray-800';
  }

  getRatingStars(rating: number | undefined): string {
    if (!rating) return '☆☆☆☆☆';
    const fullStars = Math.floor(rating);
    const halfStar = rating % 1 >= 0.5 ? '½' : '';
    const emptyStars = 5 - Math.ceil(rating);
    return '★'.repeat(fullStars) + halfStar + '☆'.repeat(emptyStars);
  }

  exportToCSV(): void {
    const headers = ['Name', 'Email', 'Phone', 'License', 'Status', 'Rating', 'Total Jobs', 'Completed Jobs'];
    const rows = this.filteredDrivers.map(driver => [
      this.getDriverFullName(driver),
      driver.email || '',
      driver.phone || '',
      driver.licenseNumber || '',
      driver.status || '',
      driver.rating?.toString() || '0',
      driver.totalJobs?.toString() || '0',
      driver.completedJobs?.toString() || '0'
    ]);

    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.map(cell => `"${cell}"`).join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `drivers-${new Date().toISOString().split('T')[0]}.csv`;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  isLicenseExpiring(driver: DriverDto): boolean {
    if (!driver.licenseExpiry) return false;
    const expiryDate = new Date(driver.licenseExpiry);
    const thirtyDaysFromNow = new Date();
    thirtyDaysFromNow.setDate(thirtyDaysFromNow.getDate() + 30);
    return expiryDate <= thirtyDaysFromNow && expiryDate > this.now;
  }

  isLicenseExpired(driver: DriverDto): boolean {
    if (!driver.licenseExpiry) return false;
    const expiryDate = new Date(driver.licenseExpiry);
    return expiryDate <= this.now;
  }
}
