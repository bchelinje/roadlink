import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  JobsService,
  JobStop,
  CreateJobStopDto,
  UpdateJobStopDto
} from '@core/api';

@Component({
  selector: 'app-job-stops',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="mb-6 flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Job Stops Management</h1>
          <p class="mt-1 text-sm text-gray-600">Manage delivery stops for Job #{{ jobId }}</p>
        </div>
        <button
          (click)="goBack()"
          class="px-4 py-2 text-gray-700 hover:text-gray-900 font-medium"
        >
          ← Back to Job
        </button>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading stops...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage && !isLoading" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Success Message -->
      <div *ngIf="successMessage" class="mb-6 bg-green-50 border border-green-200 rounded-lg p-4">
        <p class="text-green-800">{{ successMessage }}</p>
      </div>

      <div *ngIf="!isLoading">
        <!-- Add New Stop -->
        <div class="bg-white border border-gray-200 rounded-lg p-6 mb-6">
          <h3 class="text-lg font-semibold text-gray-900 mb-4">Add New Stop</h3>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Stop Order *</label>
              <input
                type="number"
                [(ngModel)]="newStop.stopOrder"
                min="1"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Stop Type *</label>
              <select
                [(ngModel)]="newStop.stopType"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg"
              >
                <option value="">Select type</option>
                <option value="Pickup">Pickup</option>
                <option value="Delivery">Delivery</option>
                <option value="Waypoint">Waypoint</option>
              </select>
            </div>

            <div class="md:col-span-2">
              <label class="block text-sm font-medium text-gray-700 mb-1">Location *</label>
              <input
                type="text"
                [(ngModel)]="newStop.location"
                placeholder="Enter address"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Contact Name</label>
              <input
                type="text"
                [(ngModel)]="newStop.contactName"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Contact Phone</label>
              <input
                type="tel"
                [(ngModel)]="newStop.contactPhone"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Scheduled Arrival</label>
              <input
                type="datetime-local"
                [(ngModel)]="newStop.scheduledArrival"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg"
              />
            </div>

            <div class="md:col-span-2">
              <label class="block text-sm font-medium text-gray-700 mb-1">Special Instructions</label>
              <textarea
                [(ngModel)]="newStop.specialInstructions"
                rows="2"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg"
              ></textarea>
            </div>
          </div>

          <button
            (click)="addStop()"
            [disabled]="isSubmitting || !newStop.stopType || !newStop.location"
            class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium text-sm disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ isSubmitting ? 'Adding...' : 'Add Stop' }}
          </button>
        </div>

        <!-- Stops List -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h3 class="text-lg font-semibold text-gray-900 mb-4">Current Stops ({{ stops.length }})</h3>

          <div *ngIf="stops.length === 0" class="text-center py-12">
            <svg class="w-24 h-24 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
            <h3 class="mt-4 text-lg font-medium text-gray-900">No stops yet</h3>
            <p class="mt-2 text-gray-600">Add stops to create a multi-stop delivery route</p>
          </div>

          <div *ngIf="stops.length > 0" class="space-y-4">
            <div
              *ngFor="let stop of stops"
              class="border border-gray-200 rounded-lg p-4"
              [class.border-green-300]="stop.status === 'Completed'"
              [class.border-blue-300]="stop.status === 'InProgress'"
            >
              <div class="flex items-start justify-between mb-3">
                <div class="flex items-center gap-3">
                  <div class="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center">
                    <span class="text-sm font-bold text-blue-600">{{ stop.stopOrder }}</span>
                  </div>
                  <div>
                    <div class="flex items-center gap-2">
                      <h4 class="font-semibold text-gray-900">{{ stop.location }}</h4>
                      <span
                        [class]="getStopTypeClass(stop.stopType)"
                        class="px-2 py-0.5 text-xs font-medium rounded"
                      >
                        {{ stop.stopType }}
                      </span>
                      <span
                        [class]="getStatusClass(stop.status)"
                        class="px-2 py-0.5 text-xs font-medium rounded"
                      >
                        {{ stop.status }}
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              <div class="grid grid-cols-1 md:grid-cols-3 gap-3 text-sm text-gray-600">
                <div *ngIf="stop.contactName">
                  <p class="text-xs text-gray-500">Contact</p>
                  <p class="font-medium text-gray-900">{{ stop.contactName }}</p>
                  <p *ngIf="stop.contactPhone" class="text-gray-600">{{ stop.contactPhone }}</p>
                </div>

                <div *ngIf="stop.scheduledArrival">
                  <p class="text-xs text-gray-500">Scheduled Arrival</p>
                  <p class="font-medium text-gray-900">{{ formatDateTime(stop.scheduledArrival) }}</p>
                </div>

                <div *ngIf="stop.actualArrival">
                  <p class="text-xs text-gray-500">Actual Arrival</p>
                  <p class="font-medium text-green-600">{{ formatDateTime(stop.actualArrival) }}</p>
                </div>
              </div>

              <div *ngIf="stop.specialInstructions" class="mt-3 text-sm text-gray-600">
                <p class="text-xs text-gray-500">Special Instructions</p>
                <p>{{ stop.specialInstructions }}</p>
              </div>

              <div *ngIf="stop.notes" class="mt-3 p-3 bg-gray-50 rounded text-sm">
                <p class="text-xs text-gray-500 mb-1">Notes</p>
                <p class="text-gray-700">{{ stop.notes }}</p>
              </div>
            </div>
          </div>
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
export class JobStopsComponent implements OnInit {
  private jobsService = inject(JobsService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  jobId = '';
  stops: JobStop[] = [];

  newStop: CreateJobStopDto = this.getEmptyStop();

  isLoading = false;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.jobId = this.route.snapshot.paramMap.get('id') || '';
    if (this.jobId) {
      this.loadStops();
    }
  }

  loadStops(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.jobsService.apiJobsIdStopsGet(this.jobId).subscribe({
      next: (stops: JobStop[]) => {
        this.stops = (stops || []).sort((a, b) => a.stopOrder - b.stopOrder);
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading stops:', error);
        this.errorMessage = 'Failed to load stops.';
        this.isLoading = false;
      }
    });
  }

  addStop(): void {
    if (!this.newStop.stopType || !this.newStop.location) {
      this.errorMessage = 'Please fill in all required fields.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.jobsService.apiJobsIdStopsPost(this.jobId, this.newStop).subscribe({
      next: () => {
        this.successMessage = 'Stop added successfully!';
        this.newStop = this.getEmptyStop();
        this.newStop.stopOrder = this.stops.length + 1;
        this.loadStops();
        this.isSubmitting = false;
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error: any) => {
        console.error('Error adding stop:', error);
        this.errorMessage = 'Failed to add stop. Please try again.';
        this.isSubmitting = false;
      }
    });
  }

  getEmptyStop(): CreateJobStopDto {
    return {
      stopOrder: this.stops.length + 1,
      stopType: null,
      location: null,
      latitude: null,
      longitude: null,
      contactName: null,
      contactPhone: null,
      specialInstructions: null,
      items: null,
      scheduledArrival: null
    };
  }

  getStopTypeClass(type: string): string {
    switch (type) {
      case 'Pickup':
        return 'bg-blue-100 text-blue-800';
      case 'Delivery':
        return 'bg-green-100 text-green-800';
      case 'Waypoint':
        return 'bg-purple-100 text-purple-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'InProgress':
        return 'bg-blue-100 text-blue-800';
      case 'Completed':
        return 'bg-green-100 text-green-800';
      case 'Skipped':
        return 'bg-gray-100 text-gray-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  formatDateTime(dateString: string | null | undefined): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  goBack(): void {
    this.router.navigate(['/jobs', this.jobId]);
  }
}
