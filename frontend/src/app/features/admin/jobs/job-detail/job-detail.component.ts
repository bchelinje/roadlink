import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { JobsService, JobDto } from '@core/api';
import { HeaderComponent } from '@app/layout/header/header.component';
import { AssignDriverModalComponent } from '@app/shared/components/assign-driver-modal/assign-driver-modal.component';

@Component({
  selector: 'app-job-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, AssignDriverModalComponent],
  templateUrl: './job-detail.component.html',
  styleUrls: ['./job-detail.component.scss']
})
export class JobDetailComponent implements OnInit {
  private jobsService = inject(JobsService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  job: JobDto | null = null;
  isLoading = false;
  errorMessage = '';
  jobId: string | null = null;
  isAssignModalOpen = false;

  ngOnInit(): void {
    this.jobId = this.route.snapshot.paramMap.get('id');
    if (this.jobId) {
      this.loadJob();
    } else {
      this.errorMessage = 'Invalid job ID';
    }
  }

  loadJob(): void {
    if (!this.jobId) return;

    this.isLoading = true;
    this.errorMessage = '';

    this.jobsService.apiJobsIdGet(this.jobId).subscribe({
      next: (job: JobDto) => {
        this.job = job;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading job:', error);
        this.errorMessage = 'Failed to load job details. Please try again.';
        this.isLoading = false;
      }
    });
  }

  editJob(): void {
    if (this.jobId) {
      this.router.navigate(['/jobs', this.jobId, 'edit']);
    }
  }

  assignDriver(): void {
    this.isAssignModalOpen = true;
  }

  onDriverAssigned(): void {
    // Reload job to get updated data
    this.loadJob();
  }

  deleteJob(): void {
    if (!this.jobId || !this.job) return;

    if (confirm(`Are you sure you want to delete job ${this.job.jobNumber}?`)) {
      this.jobsService.apiJobsIdDelete(this.jobId).subscribe({
        next: () => {
          this.router.navigate(['/jobs']);
        },
        error: (error) => {
          console.error('Error deleting job:', error);
          alert('Failed to delete job. ' + (error.error?.title || 'Please try again.'));
        }
      });
    }
  }

  backToList(): void {
    this.router.navigate(['/jobs']);
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

  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      month: 'long',
      day: 'numeric',
      year: 'numeric'
    });
  }

  formatDateTime(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  }

  formatJobType(jobType: string | null | undefined): string {
    if (!jobType) return 'N/A';
    return jobType.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  formatVehicleType(vehicleType: string | null | undefined): string {
    if (!vehicleType) return 'Not specified';
    return vehicleType.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  formatPriority(priority: string | null | undefined): string {
    if (!priority) return 'N/A';
    return priority.charAt(0).toUpperCase() + priority.slice(1);
  }

  formatStatus(status: string | null | undefined): string {
    if (!status) return 'N/A';
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }
}
