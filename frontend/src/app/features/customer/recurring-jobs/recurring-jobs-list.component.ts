import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { RecurringJobsService, RecurringJob, UpdateRecurringJobStatusDto } from '@core/api';

@Component({
  selector: 'app-recurring-jobs-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="page-header">
        <div>
          <h1>Recurring Jobs</h1>
          <p class="subtitle">Manage your scheduled recurring deliveries</p>
        </div>
        <button
          routerLink="/customer/recurring-jobs/create"
          class="btn-primary"
        >
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
          </svg>
          Create Recurring Job
        </button>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading recurring jobs...</p>
      </div>

      <!-- Success Message -->
      <div *ngIf="successMessage" class="mb-6 bg-green-50 border border-green-200 rounded-lg p-4">
        <p class="text-green-800">{{ successMessage }}</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage && !isLoading" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Empty State -->
      <div *ngIf="!isLoading && recurringJobs.length === 0 && !errorMessage" class="text-center py-12">
        <svg class="w-24 h-24 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
        <h3 class="mt-4 text-lg font-medium text-gray-900">No recurring jobs yet</h3>
        <p class="mt-2 text-gray-600">Set up recurring schedules for jobs you need regularly</p>
        <button
          routerLink="/customer/recurring-jobs/create"
          class="mt-4 px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors"
        >
          Create Your First Recurring Job
        </button>
      </div>

      <!-- Recurring Jobs List -->
      <div *ngIf="!isLoading && recurringJobs.length > 0" class="space-y-6">
        <div
          *ngFor="let job of recurringJobs"
          class="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-lg transition-shadow"
          [class.opacity-60]="job.status === 'Paused' || job.status === 'Cancelled'"
        >
          <!-- Header -->
          <div class="flex items-start justify-between mb-4">
            <div class="flex-1">
              <div class="flex items-center gap-3 mb-2">
                <h3 class="text-lg font-semibold text-gray-900">{{ job.name }}</h3>
                <span
                  [class]="getStatusClass(job.status)"
                  class="px-2 py-0.5 text-xs font-medium rounded"
                >
                  {{ job.status }}
                </span>
              </div>
              <p *ngIf="job.description" class="text-sm text-gray-600">
                {{ job.description }}
              </p>
            </div>

            <!-- Quick Actions -->
            <div class="flex items-center gap-2">
              <button
                *ngIf="job.status === 'Active'"
                (click)="pauseJob(job)"
                class="px-3 py-1.5 text-orange-600 hover:text-orange-700 text-sm font-medium"
                title="Pause"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 9v6m4-6v6m7-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </button>
              <button
                *ngIf="job.status === 'Paused'"
                (click)="resumeJob(job)"
                class="px-3 py-1.5 text-green-600 hover:text-green-700 text-sm font-medium"
                title="Resume"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </button>
            </div>
          </div>

          <!-- Job Details -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
            <!-- Frequency -->
            <div class="flex items-start gap-2">
              <svg class="w-5 h-5 text-gray-400 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <div>
                <p class="text-xs text-gray-500">Frequency</p>
                <p class="text-sm font-medium text-gray-900">{{ getFrequencyDisplay(job) }}</p>
              </div>
            </div>

            <!-- Job Type -->
            <div class="flex items-start gap-2">
              <svg class="w-5 h-5 text-gray-400 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
              </svg>
              <div>
                <p class="text-xs text-gray-500">Job Type</p>
                <p class="text-sm font-medium text-gray-900">{{ job.jobType }}</p>
              </div>
            </div>

            <!-- Route -->
            <div class="flex items-start gap-2 md:col-span-2">
              <svg class="w-5 h-5 text-gray-400 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              <div class="flex-1 min-w-0">
                <p class="text-xs text-gray-500">Route</p>
                <p class="text-sm text-gray-900 truncate">{{ job.pickupLocation }}</p>
                <p class="text-xs text-gray-500 mt-0.5">↓</p>
                <p class="text-sm text-gray-900 truncate">{{ job.deliveryLocation }}</p>
              </div>
            </div>
          </div>

          <!-- Schedule Info -->
          <div class="flex items-center gap-6 pt-4 border-t border-gray-200 text-xs text-gray-600">
            <div class="flex items-center gap-1">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              <span>Next: {{ formatDate(job.nextScheduledDate) }}</span>
            </div>
            <div class="flex items-center gap-1">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              <span>Created {{ job.jobsCreated || 0 }} jobs</span>
            </div>
            <div *ngIf="job.endDate" class="flex items-center gap-1">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span>Ends: {{ formatDate(job.endDate) }}</span>
            </div>
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-2 mt-4 pt-4 border-t border-gray-200">
            <button
              [routerLink]="['/customer/recurring-jobs/edit', job.id]"
              class="px-4 py-2 text-gray-700 hover:text-gray-900 rounded-lg font-medium text-sm transition-colors"
            >
              Edit
            </button>
            <button
              (click)="deleteJob(job)"
              class="px-4 py-2 text-red-600 hover:text-red-700 rounded-lg font-medium text-sm transition-colors"
            >
              Delete
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      padding: 2rem;
      background: linear-gradient(135deg, #003d82 0%, #001f3f 100%);
      border-radius: 1rem;
      color: white;
    }
    .page-header h1 {
      margin: 0 0 0.5rem 0;
      font-size: 2rem;
      font-weight: 700;
    }
    .page-header .subtitle {
      margin: 0;
      opacity: 0.9;
      font-size: 1rem;
    }
    .page-header .btn-primary {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.875rem 1.5rem;
      background: white;
      color: #003d82;
      border: none;
      border-radius: 0.5rem;
      font-weight: 600;
      text-decoration: none;
      transition: all 0.2s;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      cursor: pointer;
    }
    .page-header .btn-primary svg {
      width: 20px;
      height: 20px;
    }
    .page-header .btn-primary:hover {
      transform: translateY(-2px);
      box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
    }
    @media (max-width: 768px) {
      .page-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 1.5rem;
      }
      .page-header .btn-primary {
        width: 100%;
        justify-content: center;
      }
    }
  `]
})
export class RecurringJobsListComponent implements OnInit {
  private recurringJobsService = inject(RecurringJobsService);

  recurringJobs: RecurringJob[] = [];
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadRecurringJobs();
  }

  loadRecurringJobs(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.recurringJobsService.apiRecurringJobsMeGet().subscribe({
      next: (jobs: RecurringJob[]) => {
        this.recurringJobs = jobs || [];
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading recurring jobs:', error);
        this.errorMessage = 'Failed to load recurring jobs. Please try again.';
        this.isLoading = false;
      }
    });
  }

  pauseJob(job: RecurringJob): void {
    const statusDto: UpdateRecurringJobStatusDto = { status: 'Paused' };

    this.recurringJobsService.apiRecurringJobsIdStatusPatch(job.id!, statusDto).subscribe({
      next: () => {
        this.successMessage = `"${job.name}" has been paused`;
        this.loadRecurringJobs();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error: any) => {
        console.error('Error pausing job:', error);
        this.errorMessage = 'Failed to pause job. Please try again.';
      }
    });
  }

  resumeJob(job: RecurringJob): void {
    const statusDto: UpdateRecurringJobStatusDto = { status: 'Active' };

    this.recurringJobsService.apiRecurringJobsIdStatusPatch(job.id!, statusDto).subscribe({
      next: () => {
        this.successMessage = `"${job.name}" has been resumed`;
        this.loadRecurringJobs();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error: any) => {
        console.error('Error resuming job:', error);
        this.errorMessage = 'Failed to resume job. Please try again.';
      }
    });
  }

  deleteJob(job: RecurringJob): void {
    if (!confirm(`Delete recurring job "${job.name}"? This will not delete jobs already created. This cannot be undone.`)) {
      return;
    }

    this.recurringJobsService.apiRecurringJobsIdDelete(job.id!).subscribe({
      next: () => {
        this.successMessage = 'Recurring job deleted successfully';
        this.loadRecurringJobs();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error: any) => {
        console.error('Error deleting job:', error);
        this.errorMessage = 'Failed to delete recurring job. Please try again.';
      }
    });
  }

  getFrequencyDisplay(job: RecurringJob): string {
    if (!job.frequency) return 'Unknown';

    let display = job.frequency;

    if (job.frequency === 'Weekly' && job.recurrenceDays) {
      display += ` on ${job.recurrenceDays}`;
    } else if (job.frequency === 'Monthly' && job.dayOfMonth) {
      display += ` on day ${job.dayOfMonth}`;
    }

    if (job.preferredTime) {
      display += ` at ${job.preferredTime}`;
    }

    return display;
  }

  getStatusClass(status: string | null | undefined): string {
    switch (status) {
      case 'Active':
        return 'bg-green-100 text-green-800';
      case 'Paused':
        return 'bg-orange-100 text-orange-800';
      case 'Cancelled':
        return 'bg-red-100 text-red-800';
      case 'Completed':
        return 'bg-gray-100 text-gray-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return 'Not set';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
