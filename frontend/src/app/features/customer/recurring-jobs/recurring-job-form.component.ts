import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { RecurringJobsService, CreateRecurringJobDto, UpdateRecurringJobDto, RecurringJob } from '@core/api';

@Component({
  selector: 'app-recurring-job-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="container mx-auto px-4 py-8 max-w-4xl">
      <div class="mb-6">
        <h1 class="text-2xl font-bold text-gray-900">
          {{ isEditMode ? 'Edit Recurring Job' : 'Create Recurring Job' }}
        </h1>
        <p class="mt-1 text-sm text-gray-600">
          {{ isEditMode ? 'Update your recurring job schedule' : 'Set up a recurring schedule for regular deliveries' }}
        </p>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage" class="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Form -->
      <form *ngIf="!isLoading" [formGroup]="jobForm" (ngSubmit)="onSubmit()" class="space-y-6">
        <!-- Basic Info -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Basic Information</h2>

          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Schedule Name <span class="text-red-600">*</span>
              </label>
              <input
                type="text"
                formControlName="name"
                placeholder="e.g., Weekly Office Delivery"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
              <textarea
                formControlName="description"
                rows="2"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              ></textarea>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">
                  Job Type <span class="text-red-600">*</span>
                </label>
                <select formControlName="jobType" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500">
                  <option value="">Select type</option>
                  <option value="Delivery">Delivery</option>
                  <option value="Pickup">Pickup</option>
                  <option value="Transfer">Transfer</option>
                  <option value="Moving">Moving</option>
                  <option value="Courier">Courier</option>
                </select>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Vehicle Type</label>
                <select formControlName="vehicleTypeRequired" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500">
                  <option [ngValue]="null">Any vehicle</option>
                  <option value="Car">Car</option>
                  <option value="Van">Van</option>
                  <option value="Truck">Truck</option>
                </select>
              </div>
            </div>
          </div>
        </div>

        <!-- Locations -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Locations</h2>

          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Pickup Location <span class="text-red-600">*</span>
              </label>
              <input
                type="text"
                formControlName="pickupLocation"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Delivery Location <span class="text-red-600">*</span>
              </label>
              <input
                type="text"
                formControlName="deliveryLocation"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
          </div>
        </div>

        <!-- Recurrence Schedule -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Schedule Settings</h2>

          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Frequency <span class="text-red-600">*</span>
              </label>
              <select formControlName="frequency" class="w-full md:w-64 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500">
                <option value="">Select frequency</option>
                <option value="Daily">Daily</option>
                <option value="Weekly">Weekly</option>
                <option value="Monthly">Monthly</option>
              </select>
            </div>

            <!-- Weekly Options -->
            <div *ngIf="jobForm.get('frequency')?.value === 'Weekly'">
              <label class="block text-sm font-medium text-gray-700 mb-2">
                Days of Week <span class="text-red-600">*</span>
              </label>
              <div class="flex flex-wrap gap-2">
                <label *ngFor="let day of daysOfWeek" class="flex items-center px-3 py-2 border border-gray-300 rounded-lg cursor-pointer hover:bg-gray-50"
                       [class.bg-blue-50]="isSelectedDay(day)" [class.border-blue-500]="isSelectedDay(day)">
                  <input type="checkbox" [value]="day" (change)="toggleDay(day)" [checked]="isSelectedDay(day)" class="sr-only">
                  <span class="text-sm font-medium" [class.text-blue-700]="isSelectedDay(day)">{{ day }}</span>
                </label>
              </div>
            </div>

            <!-- Monthly Options -->
            <div *ngIf="jobForm.get('frequency')?.value === 'Monthly'">
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Day of Month <span class="text-red-600">*</span>
              </label>
              <input
                type="number"
                formControlName="dayOfMonth"
                min="1"
                max="31"
                placeholder="1-31"
                class="w-full md:w-64 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Preferred Time
              </label>
              <input
                type="time"
                formControlName="preferredTime"
                class="w-full md:w-64 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">
                  Start Date <span class="text-red-600">*</span>
                </label>
                <input
                  type="date"
                  formControlName="startDate"
                  [min]="minDate"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">
                  End Date (Optional)
                </label>
                <input
                  type="date"
                  formControlName="endDate"
                  [min]="jobForm.get('startDate')?.value"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Maximum Occurrences (Optional)
              </label>
              <input
                type="number"
                formControlName="occurrenceCount"
                min="1"
                placeholder="Leave blank for unlimited"
                class="w-full md:w-64 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
              <p class="mt-1 text-xs text-gray-500">Job will stop after this many occurrences</p>
            </div>
          </div>
        </div>

        <!-- Additional Details -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Additional Details</h2>

          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Special Instructions
              </label>
              <textarea
                formControlName="specialInstructions"
                rows="3"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              ></textarea>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Priority
              </label>
              <select formControlName="priority" class="w-full md:w-64 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500">
                <option [ngValue]="null">Normal</option>
                <option value="Low">Low</option>
                <option value="Normal">Normal</option>
                <option value="High">High</option>
                <option value="Urgent">Urgent</option>
              </select>
            </div>
          </div>
        </div>

        <!-- Actions -->
        <div class="flex items-center justify-end gap-3 pt-4">
          <button
            type="button"
            routerLink="/customer/recurring-jobs"
            class="px-6 py-2 text-gray-700 hover:text-gray-900 font-medium"
          >
            Cancel
          </button>
          <button
            type="submit"
            [disabled]="jobForm.invalid || isSaving"
            class="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ isSaving ? 'Saving...' : (isEditMode ? 'Update Schedule' : 'Create Schedule') }}
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class RecurringJobFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private recurringJobsService = inject(RecurringJobsService);

  jobForm!: FormGroup;
  isEditMode = false;
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  jobId: string | null = null;
  minDate = new Date().toISOString().split('T')[0];

  daysOfWeek = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  selectedDays: string[] = [];

  ngOnInit(): void {
    this.initForm();
    this.checkEditMode();
  }

  initForm(): void {
    this.jobForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      jobType: ['', Validators.required],
      vehicleTypeRequired: [null],
      priority: [null],
      pickupLocation: ['', Validators.required],
      deliveryLocation: ['', Validators.required],
      distance: [null],
      specialInstructions: [''],
      frequency: ['', Validators.required],
      dayOfMonth: [null],
      preferredTime: [''],
      startDate: ['', Validators.required],
      endDate: [''],
      occurrenceCount: [null]
    });
  }

  checkEditMode(): void {
    this.jobId = this.route.snapshot.paramMap.get('id');
    if (this.jobId) {
      this.isEditMode = true;
      this.loadJob();
    }
  }

  loadJob(): void {
    if (!this.jobId) return;

    this.isLoading = true;

    this.recurringJobsService.apiRecurringJobsIdGet(this.jobId).subscribe({
      next: (job: RecurringJob) => {
        this.jobForm.patchValue({
          name: job.name,
          description: job.description,
          jobType: job.jobType,
          vehicleTypeRequired: job.vehicleTypeRequired,
          priority: job.priority,
          pickupLocation: job.pickupLocation,
          deliveryLocation: job.deliveryLocation,
          distance: job.distance,
          specialInstructions: job.specialInstructions,
          frequency: job.frequency,
          dayOfMonth: job.dayOfMonth,
          preferredTime: job.preferredTime,
          startDate: job.startDate,
          endDate: job.endDate,
          occurrenceCount: job.occurrenceCount
        });

        if (job.recurrenceDays) {
          this.selectedDays = job.recurrenceDays.split(',').map(d => d.trim());
        }

        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading job:', error);
        this.errorMessage = 'Failed to load recurring job.';
        this.isLoading = false;
      }
    });
  }

  toggleDay(day: string): void {
    const index = this.selectedDays.indexOf(day);
    if (index > -1) {
      this.selectedDays.splice(index, 1);
    } else {
      this.selectedDays.push(day);
    }
  }

  isSelectedDay(day: string): boolean {
    return this.selectedDays.includes(day);
  }

  onSubmit(): void {
    if (this.jobForm.invalid) {
      this.jobForm.markAllAsTouched();
      return;
    }

    const formValue = this.jobForm.value;

    // Validate weekly selection
    if (formValue.frequency === 'Weekly' && this.selectedDays.length === 0) {
      this.errorMessage = 'Please select at least one day for weekly recurrence';
      return;
    }

    // Validate monthly day
    if (formValue.frequency === 'Monthly' && !formValue.dayOfMonth) {
      this.errorMessage = 'Please select day of month for monthly recurrence';
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    if (this.isEditMode && this.jobId) {
      const updateDto: UpdateRecurringJobDto = {
        name: formValue.name,
        description: formValue.description || null,
        specialInstructions: formValue.specialInstructions || null,
        preferredTime: formValue.preferredTime || null,
        endDate: formValue.endDate || null,
        occurrenceCount: formValue.occurrenceCount
      };

      this.recurringJobsService.apiRecurringJobsIdPut(this.jobId, updateDto).subscribe({
        next: () => {
          this.router.navigate(['/customer/recurring-jobs']);
        },
        error: (error: any) => {
          console.error('Error updating job:', error);
          this.errorMessage = 'Failed to update recurring job.';
          this.isSaving = false;
        }
      });
    } else {
      const createDto: CreateRecurringJobDto = {
        name: formValue.name,
        description: formValue.description || null,
        jobType: formValue.jobType,
        vehicleTypeRequired: formValue.vehicleTypeRequired,
        priority: formValue.priority,
        pickupLocation: formValue.pickupLocation,
        deliveryLocation: formValue.deliveryLocation,
        distance: formValue.distance,
        items: null,
        specialInstructions: formValue.specialInstructions || null,
        frequency: formValue.frequency,
        recurrenceDays: formValue.frequency === 'Weekly' ? this.selectedDays : null,
        dayOfMonth: formValue.frequency === 'Monthly' ? formValue.dayOfMonth : null,
        preferredTime: formValue.preferredTime || null,
        startDate: formValue.startDate,
        endDate: formValue.endDate || null,
        occurrenceCount: formValue.occurrenceCount
      };

      this.recurringJobsService.apiRecurringJobsPost(createDto).subscribe({
        next: () => {
          this.router.navigate(['/customer/recurring-jobs']);
        },
        error: (error: any) => {
          console.error('Error creating job:', error);
          this.errorMessage = 'Failed to create recurring job.';
          this.isSaving = false;
        }
      });
    }
  }
}
