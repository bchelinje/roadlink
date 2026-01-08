import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  JobsService,
  BulkJobCreationDto,
  BulkJobRequestItem,
  BulkJobCreationResponse
} from '@core/api';

@Component({
  selector: 'app-bulk-job-creation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="page-header">
        <div>
          <h1>Bulk Job Creation</h1>
          <p class="subtitle">Create multiple jobs at once</p>
        </div>
      </div>

      <!-- Success Message -->
      <div *ngIf="successMessage" class="mb-6 bg-green-50 border border-green-200 rounded-lg p-4">
        <p class="text-green-800">{{ successMessage }}</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Result Summary -->
      <div *ngIf="creationResult" class="mb-6 bg-blue-50 border border-blue-200 rounded-lg p-6">
        <h3 class="text-lg font-semibold text-blue-900 mb-4">Bulk Creation Results</h3>
        <div class="grid grid-cols-2 gap-4 mb-4">
          <div>
            <p class="text-sm text-blue-600">Total Requested</p>
            <p class="text-2xl font-bold text-blue-900">{{ creationResult.totalRequested }}</p>
          </div>
          <div>
            <p class="text-sm text-green-600">Successfully Created</p>
            <p class="text-2xl font-bold text-green-600">{{ creationResult.successCount }}</p>
          </div>
        </div>
        <div *ngIf="creationResult.errors && creationResult.errors.length > 0" class="mt-4">
          <p class="text-sm font-medium text-red-600 mb-2">Errors:</p>
          <ul class="list-disc list-inside text-sm text-red-700 space-y-1">
            <li *ngFor="let error of creationResult.errors">{{ error }}</li>
          </ul>
        </div>
        <button
          (click)="resetForm()"
          class="mt-4 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium text-sm"
        >
          Create More Jobs
        </button>
      </div>

      <!-- Form -->
      <div *ngIf="!creationResult" class="bg-white border border-gray-200 rounded-lg p-6">
        <!-- Customer ID (Optional) -->
        <div class="mb-6">
          <label class="block text-sm font-medium text-gray-700 mb-2">
            Customer ID (Optional - leave empty to create for multiple customers)
          </label>
          <input
            type="text"
            [(ngModel)]="customerId"
            placeholder="Enter customer ID if all jobs are for the same customer"
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        <!-- Jobs List -->
        <div class="mb-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-semibold text-gray-900">Jobs ({{ jobs.length }})</h3>
            <button
              (click)="addJob()"
              class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium text-sm"
            >
              + Add Job
            </button>
          </div>

          <div class="space-y-4">
            <div
              *ngFor="let job of jobs; let i = index"
              class="border border-gray-200 rounded-lg p-4"
            >
              <div class="flex items-center justify-between mb-4">
                <h4 class="font-medium text-gray-900">Job {{ i + 1 }}</h4>
                <button
                  (click)="removeJob(i)"
                  class="text-red-600 hover:text-red-700 text-sm font-medium"
                >
                  Remove
                </button>
              </div>

              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <!-- Customer Info (if no global customer ID) -->
                <div *ngIf="!customerId">
                  <label class="block text-sm font-medium text-gray-700 mb-1">Customer Name</label>
                  <input
                    type="text"
                    [(ngModel)]="job.customerName"
                    class="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  />
                </div>
                <div *ngIf="!customerId">
                  <label class="block text-sm font-medium text-gray-700 mb-1">Customer Email</label>
                  <input
                    type="email"
                    [(ngModel)]="job.customerEmail"
                    class="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  />
                </div>
                <div *ngIf="!customerId">
                  <label class="block text-sm font-medium text-gray-700 mb-1">Customer Phone</label>
                  <input
                    type="tel"
                    [(ngModel)]="job.customerPhone"
                    class="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  />
                </div>

                <!-- Job Details -->
                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-1">Job Type *</label>
                  <select
                    [(ngModel)]="job.jobType"
                    class="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  >
                    <option value="">Select type</option>
                    <option value="Standard">Standard</option>
                    <option value="Express">Express</option>
                    <option value="Scheduled">Scheduled</option>
                    <option value="Fragile">Fragile</option>
                  </select>
                </div>

                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-1">Vehicle Type</label>
                  <select
                    [(ngModel)]="job.vehicleTypeRequired"
                    class="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  >
                    <option value="">Any</option>
                    <option value="Van">Van</option>
                    <option value="Truck">Truck</option>
                    <option value="Motorcycle">Motorcycle</option>
                    <option value="Car">Car</option>
                  </select>
                </div>

                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-1">Priority</label>
                  <select
                    [(ngModel)]="job.priority"
                    class="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  >
                    <option value="Normal">Normal</option>
                    <option value="High">High</option>
                    <option value="Urgent">Urgent</option>
                  </select>
                </div>

                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-1">Scheduled Date *</label>
                  <input
                    type="date"
                    [(ngModel)]="job.scheduledDate"
                    class="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  />
                </div>

                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-1">Scheduled Time</label>
                  <input
                    type="time"
                    [(ngModel)]="job.scheduledTime"
                    class="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  />
                </div>

                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-1">Pickup Location *</label>
                  <input
                    type="text"
                    [(ngModel)]="job.pickupLocation"
                    class="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  />
                </div>

                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-1">Delivery Location *</label>
                  <input
                    type="text"
                    [(ngModel)]="job.deliveryLocation"
                    class="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  />
                </div>

                <div class="md:col-span-2">
                  <label class="block text-sm font-medium text-gray-700 mb-1">Special Instructions</label>
                  <textarea
                    [(ngModel)]="job.specialInstructions"
                    rows="2"
                    class="w-full px-3 py-2 border border-gray-300 rounded-lg"
                  ></textarea>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Actions -->
        <div class="flex items-center gap-3">
          <button
            (click)="submitBulkJobs()"
            [disabled]="isSubmitting || jobs.length === 0"
            class="px-6 py-3 bg-green-600 hover:bg-green-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ isSubmitting ? 'Creating Jobs...' : 'Create ' + jobs.length + ' Job(s)' }}
          </button>
          <button
            (click)="resetForm()"
            [disabled]="isSubmitting"
            class="px-6 py-3 text-gray-700 hover:text-gray-900 font-medium"
          >
            Reset
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class BulkJobCreationComponent {
  private jobsService = inject(JobsService);

  customerId = '';
  jobs: BulkJobRequestItem[] = [];

  isSubmitting = false;
  errorMessage = '';
  successMessage = '';
  creationResult: BulkJobCreationResponse | null = null;

  constructor() {
    // Start with one empty job
    this.addJob();
  }

  addJob(): void {
    this.jobs.push({
      customerName: null,
      customerEmail: null,
      customerPhone: null,
      jobType: null,
      vehicleTypeRequired: null,
      priority: 'Normal',
      scheduledDate: new Date().toISOString().split('T')[0],
      scheduledTime: null,
      estimatedDuration: null,
      pickupLocation: null,
      deliveryLocation: null,
      distance: null,
      items: null,
      specialInstructions: null,
      customerNotes: null
    });
  }

  removeJob(index: number): void {
    if (this.jobs.length > 1) {
      this.jobs.splice(index, 1);
    }
  }

  submitBulkJobs(): void {
    if (!this.validateJobs()) {
      this.errorMessage = 'Please fill in all required fields for each job.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const bulkDto: BulkJobCreationDto = {
      customerId: this.customerId || null,
      jobs: this.jobs
    };

    this.jobsService.apiJobsBulkPost(bulkDto).subscribe({
      next: (result: BulkJobCreationResponse) => {
        this.creationResult = result;
        this.isSubmitting = false;
        if (result.successCount === result.totalRequested) {
          this.successMessage = `Successfully created ${result.successCount} job(s)!`;
        }
      },
      error: (error: any) => {
        console.error('Error creating bulk jobs:', error);
        this.errorMessage = 'Failed to create jobs. Please try again.';
        this.isSubmitting = false;
      }
    });
  }

  validateJobs(): boolean {
    for (const job of this.jobs) {
      if (!job.jobType || !job.scheduledDate || !job.pickupLocation || !job.deliveryLocation) {
        return false;
      }
      // If no global customer ID, each job must have customer info
      if (!this.customerId && (!job.customerName && !job.customerEmail)) {
        return false;
      }
    }
    return true;
  }

  resetForm(): void {
    this.jobs = [];
    this.customerId = '';
    this.creationResult = null;
    this.errorMessage = '';
    this.successMessage = '';
    this.addJob();
  }
}
