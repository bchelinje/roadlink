import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormControl, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { JobsService, JobDto, JobDtoPaginatedResult } from '@core/api';
import { HeaderComponent } from '@app/layout/header/header.component';
import { LayoutModule } from '@angular/cdk/layout';
import { AssignDriverModalComponent } from '@app/shared/components/assign-driver-modal/assign-driver-modal.component';

@Component({
  selector: 'app-job-list',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, FormsModule, HeaderComponent, LayoutModule, AssignDriverModalComponent],
  templateUrl: './job-list.component.html',
  styleUrls: ['./job-list.component.scss']
})
export class JobListComponent implements OnInit {
  private jobsService = inject(JobsService);
  private router = inject(Router);

  jobs: JobDto[] = [];
  isLoading = false;
  errorMessage = '';
  isAssignModalOpen = false;
  selectedJobId: string | null = null;

  // Expose Math to template
  Math = Math;

  // Pagination (from backend)
  currentPage = 1;
  pageSize = 10;
  totalItems = 0;
  totalPages = 0;
  pageSizes = [5, 10, 25, 50];

  // Search
  searchControl = new FormControl('');

  // Filters
  statusFilter = new FormControl('all');
  jobTypeFilter = new FormControl('all');
  vehicleTypeFilter = new FormControl('all');
  priorityFilter = new FormControl('all');

  statuses = ['all', 'pending', 'assigned', 'confirmed', 'in_progress', 'completed', 'cancelled', 'on_hold'];
  jobTypes = ['all', 'local_move', 'long_distance', 'commercial', 'residential'];
  vehicleTypes = ['all', 'van', 'cargo_van', 'small_truck', 'large_truck', 'box_truck'];
  priorities = ['all', 'low', 'normal', 'high', 'urgent'];

  // Date filters
  startDateFilter = new FormControl('');
  endDateFilter = new FormControl('');

  ngOnInit(): void {
    this.loadJobs();
    this.setupSearch();
    this.setupFilters();
  }

  setupSearch(): void {
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadJobs();
    });
  }

  setupFilters(): void {
    this.statusFilter.valueChanges.subscribe(() => {
      this.currentPage = 1;
      this.loadJobs();
    });

    this.jobTypeFilter.valueChanges.subscribe(() => {
      this.currentPage = 1;
      this.loadJobs();
    });

    this.vehicleTypeFilter.valueChanges.subscribe(() => {
      this.currentPage = 1;
      this.loadJobs();
    });

    this.priorityFilter.valueChanges.subscribe(() => {
      this.currentPage = 1;
      this.loadJobs();
    });

    this.startDateFilter.valueChanges.subscribe(() => {
      this.currentPage = 1;
      this.loadJobs();
    });

    this.endDateFilter.valueChanges.subscribe(() => {
      this.currentPage = 1;
      this.loadJobs();
    });
  }

  loadJobs(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const status = this.statusFilter.value === 'all' ? undefined : (this.statusFilter.value || undefined);
    const jobType = this.jobTypeFilter.value === 'all' ? undefined : (this.jobTypeFilter.value || undefined);
    const vehicleType = this.vehicleTypeFilter.value === 'all' ? undefined : (this.vehicleTypeFilter.value || undefined);
    const priority = this.priorityFilter.value === 'all' ? undefined : (this.priorityFilter.value || undefined);
    const searchTerm = this.searchControl.value || undefined;
    const startDate = this.startDateFilter.value || undefined;
    const endDate = this.endDateFilter.value || undefined;

    this.jobsService.apiJobsGet(status, jobType, vehicleType, undefined, undefined, startDate, endDate, priority, searchTerm, this.currentPage, this.pageSize).subscribe({
      next: (result: JobDtoPaginatedResult) => {
        this.jobs = result.items || [];
        this.totalItems = result.total || 0;
        this.totalPages = Math.ceil(this.totalItems / this.pageSize);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading jobs:', error);
        this.errorMessage = 'Failed to load jobs. Please try again.';
        this.isLoading = false;
      }
    });
  }

  onPageSizeChange(): void {
    this.currentPage = 1;
    this.loadJobs();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadJobs();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadJobs();
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadJobs();
    }
  }

  viewJob(id: string): void {
    this.router.navigate(['/jobs', id]);
  }

  editJob(id: string): void {
    this.router.navigate(['/jobs', id, 'edit']);
  }

  createJob(): void {
    this.router.navigate(['/jobs', 'create']);
  }

  assignJob(job: JobDto): void {
    this.selectedJobId = job.id || null;
    this.isAssignModalOpen = true;
  }

  onDriverAssigned(): void {
    // Reload jobs to get updated data
    this.loadJobs();
  }

  deleteJob(job: JobDto): void {
    if (confirm(`Are you sure you want to delete job ${job.jobNumber}?`)) {
      this.jobsService.apiJobsIdDelete(job.id!).subscribe({
        next: () => {
          this.loadJobs();
        },
        error: (error) => {
          console.error('Error deleting job:', error);
          alert('Failed to delete job. ' + (error.error?.title || 'Please try again.'));
        }
      });
    }
  }

  getStatusBadgeClass(status: string | null | undefined): string {
    if (!status) return 'bg-gray-100 text-gray-800';
    const statusMap: { [key: string]: string } = {
      'pending': 'bg-yellow-100 text-yellow-800',
      'assigned': 'bg-blue-100 text-blue-800',
      'confirmed': 'bg-indigo-100 text-indigo-800',
      'in_progress': 'bg-purple-100 text-purple-800',
      'completed': 'bg-green-100 text-green-800',
      'cancelled': 'bg-red-100 text-red-800',
      'on_hold': 'bg-gray-100 text-gray-800'
    };
    return statusMap[status] || 'bg-gray-100 text-gray-800';
  }

  getPriorityBadgeClass(priority: string | null | undefined): string {
    if (!priority) return 'bg-gray-100 text-gray-600';
    const priorityMap: { [key: string]: string } = {
      'low': 'bg-gray-100 text-gray-600',
      'normal': 'bg-blue-100 text-blue-600',
      'high': 'bg-orange-100 text-orange-600',
      'urgent': 'bg-red-100 text-red-600'
    };
    return priorityMap[priority] || 'bg-gray-100 text-gray-600';
  }

  clearFilters(): void {
    this.statusFilter.setValue('all');
    this.jobTypeFilter.setValue('all');
    this.vehicleTypeFilter.setValue('all');
    this.priorityFilter.setValue('all');
    this.startDateFilter.setValue('');
    this.endDateFilter.setValue('');
    this.searchControl.setValue('');
    this.currentPage = 1;
    this.loadJobs();
  }
}
