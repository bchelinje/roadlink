import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CustomerJobsService, JobDto, CancelJobDto } from '@core/api';
import { AuthService } from '@core/services/auth.service';
import { ToastService } from '@core/services/toast.service';
import { JobBidsListComponent } from './components/job-bids-list.component';

interface TimelineEvent {
  status: string;
  label: string;
  timestamp?: string;
  isCompleted: boolean;
  isCurrent: boolean;
  icon: string;
}

@Component({
  selector: 'app-job-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, FormsModule, JobBidsListComponent],
  templateUrl: './job-detail.component.html',
  styleUrls: ['./job-detail.component.scss']
})
export class JobDetailComponent implements OnInit {
  private customerJobsService = inject(CustomerJobsService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastService = inject(ToastService);

  currentUser$ = this.authService.currentUser$;
  job: JobDto | null = null;
  isLoading = true;
  errorMessage = '';
  jobId: string | null = null;

  timelineEvents: TimelineEvent[] = [];

  // Cancellation
  showCancelModal = false;
  cancellationReason = '';
  isCancelling = false;

  ngOnInit(): void {
    this.jobId = this.route.snapshot.paramMap.get('id');
    if (this.jobId) {
      this.loadJobDetails(this.jobId);
    } else {
      this.errorMessage = 'Job ID not provided';
      this.isLoading = false;
    }
  }

  loadJobDetails(jobId: string): void {
    this.isLoading = true;
    this.errorMessage = '';

    // Use CustomerJobsService which automatically filters by authenticated customer
    this.customerJobsService.apiCustomersJobsIdGet(jobId).subscribe({
      next: (job: JobDto) => {
        this.job = job;
        this.buildTimeline(job);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading job details:', error);
        this.errorMessage = error.status === 404
          ? 'Job not found or you do not have permission to view it'
          : 'Failed to load job details';
        this.isLoading = false;
      }
    });
  }

  buildTimeline(job: JobDto): void {
    const statusOrder = ['pending', 'assigned', 'confirmed', 'in_progress', 'completed'];
    const currentStatusIndex = statusOrder.indexOf(job.status || '');

    this.timelineEvents = [
      {
        status: 'pending',
        label: 'Job Requested',
        timestamp: job.createdAt,
        isCompleted: currentStatusIndex >= 0,
        isCurrent: job.status === 'pending',
        icon: 'clock'
      },
      {
        status: 'assigned',
        label: 'Driver Assigned',
        timestamp: this.getStatusTimestamp(job, 'assigned'),
        isCompleted: currentStatusIndex >= 1,
        isCurrent: job.status === 'assigned',
        icon: 'user-check'
      },
      {
        status: 'confirmed',
        label: 'Job Confirmed',
        timestamp: this.getStatusTimestamp(job, 'confirmed'),
        isCompleted: currentStatusIndex >= 2,
        isCurrent: job.status === 'confirmed',
        icon: 'check-circle'
      },
      {
        status: 'in_progress',
        label: 'In Progress',
        timestamp: job.actualStartTime || this.getStatusTimestamp(job, 'in_progress'),
        isCompleted: currentStatusIndex >= 3,
        isCurrent: job.status === 'in_progress',
        icon: 'truck'
      },
      {
        status: 'completed',
        label: 'Completed',
        timestamp: job.completedAt || this.getStatusTimestamp(job, 'completed'),
        isCompleted: currentStatusIndex >= 4,
        isCurrent: job.status === 'completed',
        icon: 'check-badge'
      }
    ];

    // Handle cancelled status
    if (job.status === 'cancelled') {
      this.timelineEvents.push({
        status: 'cancelled',
        label: 'Cancelled',
        timestamp: this.getStatusTimestamp(job, 'cancelled'),
        isCompleted: true,
        isCurrent: true,
        icon: 'x-circle'
      });
    }
  }

  getStatusTimestamp(job: JobDto, status: string): string | undefined {
    if (!job.statusHistory) return undefined;

    try {
      const history = JSON.parse(job.statusHistory);
      const statusEntry = history.find((entry: any) => entry.status === status);
      return statusEntry?.timestamp;
    } catch {
      return undefined;
    }
  }

  parseLocation(locationJson: string | null | undefined): any {
    if (!locationJson) return null;
    try {
      return JSON.parse(locationJson);
    } catch {
      return null;
    }
  }

  formatLocation(locationJson: string | null | undefined): string {
    if (!locationJson) return 'N/A';

    // Backend now sends plain address strings, not JSON
    // If it's already a plain string, just return it
    if (!locationJson.startsWith('{')) {
      return locationJson;
    }

    // Try parsing as JSON (for backward compatibility)
    const loc = this.parseLocation(locationJson);
    if (!loc) return locationJson; // Return raw string if parsing fails

    const parts = [];
    if (loc.address) parts.push(loc.address);
    if (loc.city) parts.push(loc.city);
    if (loc.state) parts.push(loc.state);
    if (loc.zipCode) parts.push(loc.zipCode);

    return parts.length > 0 ? parts.join(', ') : locationJson;
  }

  parseItems(itemsJson: string | null | undefined): any[] {
    if (!itemsJson) return [];
    try {
      const parsed = JSON.parse(itemsJson);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  formatDate(date: string | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric'
    });
  }

  formatTime(time: string | null | undefined): string {
    if (!time) return 'N/A';
    return time;
  }

  formatDateTime(datetime: string | undefined): string {
    if (!datetime) return 'N/A';
    return new Date(datetime).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
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

  getPriorityClass(priority: string | null | undefined): string {
    if (!priority) return 'priority-normal';
    const priorityMap: { [key: string]: string } = {
      'low': 'priority-low',
      'normal': 'priority-normal',
      'high': 'priority-high',
      'urgent': 'priority-urgent'
    };
    return priorityMap[priority] || 'priority-normal';
  }

  goBack(): void {
    this.router.navigate(['/customer/my-jobs']);
  }

  canCancelJob(): boolean {
    if (!this.job || !this.job.status) return false;
    const cancellableStatuses = ['pending', 'assigned', 'confirmed'];
    return cancellableStatuses.includes(this.job.status);
  }

  openCancelModal(): void {
    this.showCancelModal = true;
    this.cancellationReason = '';
  }

  closeCancelModal(): void {
    this.showCancelModal = false;
    this.cancellationReason = '';
  }

  confirmCancellation(): void {
    if (!this.jobId) return;

    if (!this.cancellationReason || this.cancellationReason.trim().length < 10) {
      this.toastService.error('Invalid Reason', 'Please provide a cancellation reason (minimum 10 characters)');
      return;
    }

    this.isCancelling = true;

    const cancelDto: CancelJobDto = {
      reason: this.cancellationReason
    };

    this.customerJobsService.apiCustomersJobsIdCancelPost(this.jobId, cancelDto).subscribe({
      next: () => {
        this.toastService.success('Job Cancelled', 'Your job has been cancelled successfully. Refund will be processed shortly.');
        this.closeCancelModal();
        this.isCancelling = false;
        // Reload job details to show updated status
        this.loadJobDetails(this.jobId!);
      },
      error: (error) => {
        console.error('Error cancelling job:', error);
        this.toastService.error('Cancellation Failed', error.error?.title || 'Failed to cancel job. Please try again.');
        this.isCancelling = false;
      }
    });
  }
}
