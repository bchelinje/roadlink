// features/driver/dashboard/dashboard.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DriversService } from '@core/api/api/drivers.service';
import { DriverStatsDto, JobDto } from '@core/api/model/models';

@Component({
  selector: 'app-driver-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DriverDashboardComponent implements OnInit {
  private readonly driversService = inject(DriversService);

  stats: DriverStatsDto | null = null;
  upcomingJobs: JobDto[] = [];
  recentJobs: JobDto[] = [];
  loading = true;
  error: string | null = null;

  // Availability toggle
  isAvailable = false;
  isTogglingAvailability = false;

  ngOnInit(): void {
    this.loadDashboardData();
    this.loadDriverProfile();
  }

  private loadDashboardData(): void {
    this.loading = true;
    this.error = null;

    // Load driver stats
    this.driversService.apiDriversMeStatsGet().subscribe({
      next: (stats: DriverStatsDto) => {
        this.stats = stats;
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Failed to load driver stats:', err);
        this.error = 'Failed to load dashboard data. Please try again.';
        this.loading = false;
      }
    });

    // Load upcoming jobs (pending and assigned)
    this.driversService.apiDriversMeJobsGet('assigned', undefined, undefined, 1, 5).subscribe({
      next: (response: any) => {
        this.upcomingJobs = response.data || [];
      },
      error: (err: any) => {
        console.error('Failed to load upcoming jobs:', err);
      }
    });

    // Load recent jobs (completed)
    this.driversService.apiDriversMeJobsGet('completed', undefined, undefined, 1, 5).subscribe({
      next: (response: any) => {
        this.recentJobs = response.data || [];
      },
      error: (err: any) => {
        console.error('Failed to load recent jobs:', err);
      }
    });
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

  private loadDriverProfile(): void {
    this.driversService.apiDriversMeGet().subscribe({
      next: (driver: any) => {
        this.isAvailable = driver.status === 'Available' || driver.status === 'Online';
      },
      error: (err: any) => {
        console.error('Failed to load driver profile:', err);
      }
    });
  }

  toggleAvailability(): void {
    this.isTogglingAvailability = true;
    const newStatus = this.isAvailable ? 'Offline' : 'Available';

    this.driversService.apiDriversMeStatusPatch({ status: newStatus }).subscribe({
      next: () => {
        this.isAvailable = !this.isAvailable;
        this.isTogglingAvailability = false;
      },
      error: (err: any) => {
        console.error('Failed to update availability:', err);
        this.error = 'Failed to update availability. Please try again.';
        this.isTogglingAvailability = false;
      }
    });
  }
}
