// features/driver/my-jobs/my-jobs.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DriversService } from '@core/api/api/drivers.service';
import { JobDto } from '@core/api/model/models';

@Component({
  selector: 'app-my-jobs',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './my-jobs.component.html',
  styleUrls: ['./my-jobs.component.scss']
})
export class MyJobsComponent implements OnInit {
  private readonly driversService = inject(DriversService);

  jobs: JobDto[] = [];
  filteredJobs: JobDto[] = [];
  loading = true;
  error: string | null = null;

  // Filters
  selectedStatus: string = 'all';
  selectedPriority: string = 'all';
  searchTerm: string = '';
  sortBy: string = 'scheduledDate';
  sortOrder: 'asc' | 'desc' = 'asc';

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalItems = 0;

  statuses = [
    { value: 'all', label: 'All Statuses' },
    { value: 'pending', label: 'Pending' },
    { value: 'assigned', label: 'Assigned' },
    { value: 'confirmed', label: 'Confirmed' },
    { value: 'in_progress', label: 'In Progress' },
    { value: 'completed', label: 'Completed' },
    { value: 'cancelled', label: 'Cancelled' },
    { value: 'on_hold', label: 'On Hold' }
  ];

  priorities = [
    { value: 'all', label: 'All Priorities' },
    { value: 'urgent', label: 'Urgent' },
    { value: 'high', label: 'High' },
    { value: 'normal', label: 'Normal' },
    { value: 'low', label: 'Low' }
  ];

  ngOnInit(): void {
    this.loadJobs();
  }

  private loadJobs(): void {
    this.loading = true;
    this.error = null;

    const status = this.selectedStatus !== 'all' ? this.selectedStatus : undefined;

    this.driversService.apiDriversMeJobsGet(status, undefined, undefined, this.currentPage, this.pageSize).subscribe({
      next: (response: any) => {
        this.jobs = response.items || [];
        this.totalItems = response.total || 0;
        this.applyFilters();
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Failed to load jobs:', err);
        this.error = 'Failed to load jobs. Please try again.';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.jobs];

    // Apply priority filter
    if (this.selectedPriority !== 'all') {
      filtered = filtered.filter(job => job.priority === this.selectedPriority);
    }

    // Apply search filter
    if (this.searchTerm.trim()) {
      const search = this.searchTerm.toLowerCase();
      filtered = filtered.filter(job =>
        job.jobNumber?.toLowerCase().includes(search) ||
        job.customerName?.toLowerCase().includes(search) ||
        job.customerEmail?.toLowerCase().includes(search)
      );
    }

    // Apply sorting
    filtered.sort((a, b) => {
      let aValue: any, bValue: any;

      switch (this.sortBy) {
        case 'scheduledDate':
          aValue = new Date(a.scheduledDate || 0).getTime();
          bValue = new Date(b.scheduledDate || 0).getTime();
          break;
        case 'jobNumber':
          aValue = a.jobNumber || '';
          bValue = b.jobNumber || '';
          break;
        case 'customerName':
          aValue = a.customerName || '';
          bValue = b.customerName || '';
          break;
        case 'priority':
          const priorityOrder: Record<string, number> = { urgent: 4, high: 3, normal: 2, low: 1 };
          aValue = priorityOrder[a.priority || 'normal'];
          bValue = priorityOrder[b.priority || 'normal'];
          break;
        default:
          return 0;
      }

      if (aValue < bValue) return this.sortOrder === 'asc' ? -1 : 1;
      if (aValue > bValue) return this.sortOrder === 'asc' ? 1 : -1;
      return 0;
    });

    this.filteredJobs = filtered;
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadJobs();
  }

  onLocalFilterChange(): void {
    this.applyFilters();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadJobs();
  }

  clearFilters(): void {
    this.selectedStatus = 'all';
    this.selectedPriority = 'all';
    this.searchTerm = '';
    this.sortBy = 'scheduledDate';
    this.sortOrder = 'asc';
    this.currentPage = 1;
    this.loadJobs();
  }

  getStatusBadgeClass(status: string): string {
    const statusMap: Record<string, string> = {
      'pending': 'bg-yellow-100 text-yellow-800',
      'assigned': 'bg-blue-100 text-blue-800',
      'confirmed': 'bg-purple-100 text-purple-800',
      'in_progress': 'bg-indigo-100 text-indigo-800',
      'completed': 'bg-green-100 text-green-800',
      'cancelled': 'bg-red-100 text-red-800',
      'on_hold': 'bg-gray-100 text-gray-800'
    };
    return statusMap[status] || 'bg-gray-100 text-gray-800';
  }

  getPriorityBadgeClass(priority: string): string {
    const priorityMap: Record<string, string> = {
      'urgent': 'bg-red-100 text-red-800',
      'high': 'bg-orange-100 text-orange-800',
      'normal': 'bg-blue-100 text-blue-800',
      'low': 'bg-gray-100 text-gray-800'
    };
    return priorityMap[priority] || 'bg-gray-100 text-gray-800';
  }

  formatStatus(status: string): string {
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  formatPriority(priority: string): string {
    return priority.charAt(0).toUpperCase() + priority.slice(1);
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  formatTime(time: string | null | undefined): string {
    if (!time) return 'N/A';
    return new Date(time).toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  }

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize);
  }

  get pages(): number[] {
    const pages: number[] = [];
    for (let i = 1; i <= this.totalPages; i++) {
      pages.push(i);
    }
    return pages;
  }
}
