import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { JobTemplatesService, JobTemplate } from '@core/api';

@Component({
  selector: 'app-job-templates-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="page-header">
        <div>
          <h1>Job Templates</h1>
          <p class="subtitle">Reusable templates for frequently booked jobs</p>
        </div>
        <button
          routerLink="/customer/job-templates/create"
          class="btn-primary"
        >
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
          </svg>
          Create Template
        </button>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading templates...</p>
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
      <div *ngIf="!isLoading && templates.length === 0 && !errorMessage" class="text-center py-12">
        <svg class="w-24 h-24 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
        <h3 class="mt-4 text-lg font-medium text-gray-900">No templates yet</h3>
        <p class="mt-2 text-gray-600">Create templates for jobs you book frequently to save time</p>
        <button
          routerLink="/customer/job-templates/create"
          class="mt-4 px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors"
        >
          Create Your First Template
        </button>
      </div>

      <!-- Templates Grid -->
      <div *ngIf="!isLoading && templates.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <div
          *ngFor="let template of templates"
          class="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-lg transition-shadow"
        >
          <!-- Template Header -->
          <div class="flex items-start justify-between mb-4">
            <div class="flex-1">
              <div class="flex items-center gap-2 mb-2">
                <h3 class="text-lg font-semibold text-gray-900">{{ template.templateName }}</h3>
                <span
                  *ngIf="template.isDefault"
                  class="px-2 py-0.5 bg-blue-100 text-blue-800 text-xs font-medium rounded"
                >
                  Default
                </span>
              </div>
              <p *ngIf="template.description" class="text-sm text-gray-600 line-clamp-2">
                {{ template.description }}
              </p>
            </div>
          </div>

          <!-- Template Details -->
          <div class="space-y-3 mb-4">
            <!-- Job Type -->
            <div class="flex items-start gap-2">
              <svg class="w-5 h-5 text-gray-400 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
              </svg>
              <div class="flex-1 min-w-0">
                <p class="text-xs text-gray-500">Job Type</p>
                <p class="text-sm font-medium text-gray-900">{{ template.jobType }}</p>
              </div>
            </div>

            <!-- Locations -->
            <div class="flex items-start gap-2">
              <svg class="w-5 h-5 text-gray-400 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              <div class="flex-1 min-w-0">
                <p class="text-xs text-gray-500">Route</p>
                <p class="text-sm text-gray-900 truncate">{{ template.pickupLocation }}</p>
                <p class="text-xs text-gray-500 mt-1">↓</p>
                <p class="text-sm text-gray-900 truncate">{{ template.deliveryLocation }}</p>
              </div>
            </div>

            <!-- Usage Stats -->
            <div class="flex items-center gap-4 pt-3 border-t border-gray-200">
              <div class="flex items-center gap-1 text-xs text-gray-600">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                </svg>
                <span>Used {{ template.timesUsed || 0 }} times</span>
              </div>
              <div *ngIf="template.lastUsedDate" class="flex items-center gap-1 text-xs text-gray-600">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>{{ formatDate(template.lastUsedDate) }}</span>
              </div>
            </div>
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-2 pt-4 border-t border-gray-200">
            <button
              (click)="createJobFromTemplate(template)"
              class="flex-1 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium text-sm transition-colors"
            >
              Use Template
            </button>
            <button
              [routerLink]="['/customer/job-templates/edit', template.id]"
              class="px-4 py-2 text-gray-700 hover:text-gray-900 rounded-lg font-medium text-sm transition-colors"
              title="Edit"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
            </button>
            <button
              (click)="deleteTemplate(template)"
              class="px-4 py-2 text-red-600 hover:text-red-700 rounded-lg font-medium text-sm transition-colors"
              title="Delete"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
            </button>
          </div>
        </div>
      </div>

      <!-- Create Job Modal -->
      <div
        *ngIf="showCreateJobModal"
        class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4"
        (click)="closeCreateJobModal()"
      >
        <div
          class="bg-white rounded-lg p-6 max-w-md w-full"
          (click)="$event.stopPropagation()"
        >
          <h2 class="text-xl font-bold text-gray-900 mb-4">Create Job from Template</h2>
          <p class="text-sm text-gray-600 mb-4">
            Schedule a job using the "{{ selectedTemplate?.templateName }}" template
          </p>

          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Scheduled Date</label>
              <input
                type="date"
                [(ngModel)]="scheduledDate"
                [min]="minDate"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Scheduled Time (Optional)</label>
              <input
                type="time"
                [(ngModel)]="scheduledTime"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
          </div>

          <div class="flex items-center gap-3 mt-6">
            <button
              (click)="closeCreateJobModal()"
              class="flex-1 px-4 py-2 text-gray-700 hover:text-gray-900 font-medium"
            >
              Cancel
            </button>
            <button
              (click)="confirmCreateJob()"
              [disabled]="!scheduledDate || isCreatingJob"
              class="flex-1 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ isCreatingJob ? 'Creating...' : 'Create Job' }}
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
    .line-clamp-2 {
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
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
export class JobTemplatesListComponent implements OnInit {
  private jobTemplatesService = inject(JobTemplatesService);

  templates: JobTemplate[] = [];
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  // Create job modal
  showCreateJobModal = false;
  selectedTemplate: JobTemplate | null = null;
  scheduledDate = '';
  scheduledTime = '';
  isCreatingJob = false;
  minDate = new Date().toISOString().split('T')[0];

  ngOnInit(): void {
    this.loadTemplates();
  }

  loadTemplates(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.jobTemplatesService.apiJobTemplatesMeGet().subscribe({
      next: (templates: JobTemplate[]) => {
        this.templates = templates || [];
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading templates:', error);
        this.errorMessage = 'Failed to load job templates. Please try again.';
        this.isLoading = false;
      }
    });
  }

  createJobFromTemplate(template: JobTemplate): void {
    this.selectedTemplate = template;
    this.scheduledDate = new Date().toISOString().split('T')[0];
    this.scheduledTime = '';
    this.showCreateJobModal = true;
  }

  closeCreateJobModal(): void {
    this.showCreateJobModal = false;
    this.selectedTemplate = null;
    this.scheduledDate = '';
    this.scheduledTime = '';
  }

  confirmCreateJob(): void {
    if (!this.selectedTemplate || !this.scheduledDate) return;

    this.isCreatingJob = true;
    this.errorMessage = '';

    this.jobTemplatesService.apiJobTemplatesIdCreateJobPost(
      this.selectedTemplate.id!,
      {
        scheduledDate: this.scheduledDate,
        scheduledTime: this.scheduledTime || null
      }
    ).subscribe({
      next: () => {
        this.successMessage = 'Job created successfully from template!';
        this.isCreatingJob = false;
        this.closeCreateJobModal();
        this.loadTemplates(); // Refresh to update usage stats
        setTimeout(() => {
          this.successMessage = '';
        }, 5000);
      },
      error: (error: any) => {
        console.error('Error creating job:', error);
        this.errorMessage = 'Failed to create job. Please try again.';
        this.isCreatingJob = false;
      }
    });
  }

  deleteTemplate(template: JobTemplate): void {
    if (!confirm(`Delete template "${template.templateName}"? This cannot be undone.`)) {
      return;
    }

    this.jobTemplatesService.apiJobTemplatesIdDelete(template.id!).subscribe({
      next: () => {
        this.successMessage = 'Template deleted successfully';
        this.loadTemplates();
        setTimeout(() => {
          this.successMessage = '';
        }, 3000);
      },
      error: (error: any) => {
        console.error('Error deleting template:', error);
        this.errorMessage = 'Failed to delete template. Please try again.';
      }
    });
  }

  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return 'Never';
    const date = new Date(dateString);
    const now = new Date();
    const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays} days ago`;
    if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`;
    return date.toLocaleDateString();
  }
}
