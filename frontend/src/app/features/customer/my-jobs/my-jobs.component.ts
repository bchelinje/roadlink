import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CustomerJobsService, JobDto } from '@core/api';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-my-jobs',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './my-jobs.component.html',
  styleUrls: ['./my-jobs.component.scss']
})
export class MyJobsComponent implements OnInit {
  private customerJobsService = inject(CustomerJobsService);
  private authService = inject(AuthService);
  private router = inject(Router);

  currentUser$ = this.authService.currentUser$;
  jobs: JobDto[] = [];
  filteredJobs: JobDto[] = [];
  isLoading = true;
  errorMessage = '';

  // Filters
  searchTerm = '';
  selectedStatus = '';
  sortBy: 'date' | 'status' = 'date';
  sortOrder: 'asc' | 'desc' = 'desc';

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalJobs = 0;

  statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'pending', label: 'Pending' },
    { value: 'assigned', label: 'Assigned' },
    { value: 'confirmed', label: 'Confirmed' },
    { value: 'in_progress', label: 'In Progress' },
    { value: 'completed', label: 'Completed' },
    { value: 'cancelled', label: 'Cancelled' }
  ];

  ngOnInit(): void {
    this.loadJobs();
  }

  loadJobs(): void {
    this.isLoading = true;
    this.errorMessage = '';

    // Use the new CustomerJobs endpoint which only returns jobs for the authenticated customer
    this.customerJobsService.apiCustomersJobsGet().subscribe({
      next: (jobs: JobDto[]) => {
        this.jobs = jobs;
        this.totalJobs = jobs.length;
        this.applyFilters();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading jobs:', error);
        this.errorMessage = 'Failed to load jobs';
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.jobs];

    // Apply search filter
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(job =>
        job.jobNumber?.toLowerCase().includes(term) ||
        job.customerName?.toLowerCase().includes(term) ||
        job.jobType?.toLowerCase().includes(term) ||
        job.specialInstructions?.toLowerCase().includes(term)
      );
    }

    // Apply status filter
    if (this.selectedStatus) {
      filtered = filtered.filter(job => job.status === this.selectedStatus);
    }

    // Apply sorting
    filtered.sort((a, b) => {
      if (this.sortBy === 'date') {
        const dateA = a.scheduledDate ? new Date(a.scheduledDate).getTime() : 0;
        const dateB = b.scheduledDate ? new Date(b.scheduledDate).getTime() : 0;
        return this.sortOrder === 'desc' ? dateB - dateA : dateA - dateB;
      } else {
        const statusA = a.status || '';
        const statusB = b.status || '';
        return this.sortOrder === 'desc'
          ? statusB.localeCompare(statusA)
          : statusA.localeCompare(statusB);
      }
    });

    this.filteredJobs = filtered;
    this.totalJobs = filtered.length;
    this.currentPage = 1; // Reset to first page when filters change
  }

  get paginatedJobs(): JobDto[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    return this.filteredJobs.slice(startIndex, endIndex);
  }

  get totalPages(): number {
    return Math.ceil(this.totalJobs / this.pageSize);
  }

  get pageNumbers(): number[] {
    const pages: number[] = [];
    const maxPagesToShow = 5;
    let startPage = Math.max(1, this.currentPage - Math.floor(maxPagesToShow / 2));
    let endPage = Math.min(this.totalPages, startPage + maxPagesToShow - 1);

    if (endPage - startPage < maxPagesToShow - 1) {
      startPage = Math.max(1, endPage - maxPagesToShow + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    return pages;
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onStatusChange(): void {
    this.applyFilters();
  }

  onSortChange(): void {
    this.applyFilters();
  }

  toggleSortOrder(): void {
    this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    this.applyFilters();
  }

  viewJobDetails(jobId: string | undefined): void {
    if (jobId) {
      this.router.navigate(['/customer/jobs', jobId]);
    }
  }

  getStatusClass(status: string | null | undefined): string {
    if (!status) return 'status-default';
    const statusMap: { [key: string]: string } = {
      'pending': 'status-pending',
      'assigned': 'status-assigned',
      'confirmed': 'status-confirmed',
      'in_progress': 'status-progress',
      'completed': 'status-completed',
      'cancelled': 'status-cancelled',
      'on_hold': 'status-hold'
    };
    return statusMap[status] || 'status-default';
  }

  getStatusLabel(status: string | null | undefined): string {
    if (!status) return 'Unknown';
    return status.split('_').map(word =>
      word.charAt(0).toUpperCase() + word.slice(1)
    ).join(' ');
  }

  formatDate(date: string | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  parseLocation(locationJson: string | null | undefined): string {
    if (!locationJson) return 'N/A';
    try {
      const loc = JSON.parse(locationJson);
      return `${loc.city}, ${loc.state}`;
    } catch {
      return 'N/A';
    }
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedStatus = '';
    this.sortBy = 'date';
    this.sortOrder = 'desc';
    this.applyFilters();
  }
}
