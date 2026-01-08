import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { JobsService, JobDto } from '@core/api';
import { AuthService } from '@core/services/auth.service';

interface JobStats {
  total: number;
  pending: number;
  assigned: number;
  inProgress: number;
  completed: number;
  cancelled: number;
}

@Component({
  selector: 'app-customer-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class CustomerDashboardComponent implements OnInit {
  private jobsService = inject(JobsService);
  private authService = inject(AuthService);

  currentUser$ = this.authService.currentUser$;
  recentJobs: JobDto[] = [];
  stats: JobStats = {
    total: 0,
    pending: 0,
    assigned: 0,
    inProgress: 0,
    completed: 0,
    cancelled: 0
  };
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    // Load all jobs for the current user
    // We'll filter on backend later, for now get all and filter client-side
    this.currentUser$.subscribe(user => {
      if (!user?.email) return;

      this.jobsService.apiJobsGet().subscribe({
        next: (response: any) => {
          // Filter jobs by customer email
          const customerJobs = response.items?.filter((job: JobDto) =>
            job.customerEmail === user.email
          ) || [];

          this.recentJobs = customerJobs.slice(0, 5); // Show 5 most recent
          this.calculateStats(customerJobs);
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading dashboard data:', error);
          this.errorMessage = 'Failed to load dashboard data';
          this.isLoading = false;
        }
      });
    });
  }

  private calculateStats(jobs: JobDto[]): void {
    this.stats = {
      total: jobs.length,
      pending: jobs.filter(j => j.status === 'pending').length,
      assigned: jobs.filter(j => j.status === 'assigned').length,
      inProgress: jobs.filter(j => j.status === 'in_progress').length,
      completed: jobs.filter(j => j.status === 'completed').length,
      cancelled: jobs.filter(j => j.status === 'cancelled').length
    };
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
}
