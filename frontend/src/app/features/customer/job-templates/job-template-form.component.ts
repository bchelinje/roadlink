import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { JobTemplatesService, CreateJobTemplateDto, UpdateJobTemplateDto, JobTemplate } from '@core/api';

@Component({
  selector: 'app-job-template-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="container mx-auto px-4 py-8 max-w-4xl">
      <div class="mb-6">
        <h1 class="text-2xl font-bold text-gray-900">
          {{ isEditMode ? 'Edit Template' : 'Create Job Template' }}
        </h1>
        <p class="mt-1 text-sm text-gray-600">
          {{ isEditMode ? 'Update your template details' : 'Save time by creating a reusable job template' }}
        </p>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading template...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage" class="mb-6 bg-red-50 border border-red-200 rounded-lg p-4">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Template Form -->
      <form *ngIf="!isLoading" [formGroup]="templateForm" (ngSubmit)="onSubmit()" class="space-y-6">
        <!-- Basic Information -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Basic Information</h2>

          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Template Name <span class="text-red-600">*</span>
              </label>
              <input
                type="text"
                formControlName="templateName"
                placeholder="e.g., Weekly Office Delivery"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                [class.border-red-500]="templateForm.get('templateName')?.invalid && templateForm.get('templateName')?.touched"
              />
              <p *ngIf="templateForm.get('templateName')?.invalid && templateForm.get('templateName')?.touched" class="mt-1 text-sm text-red-600">
                Template name is required
              </p>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Description
              </label>
              <textarea
                formControlName="description"
                rows="3"
                placeholder="Brief description of this template"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              ></textarea>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">
                  Job Type <span class="text-red-600">*</span>
                </label>
                <select
                  formControlName="jobType"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  [class.border-red-500]="templateForm.get('jobType')?.invalid && templateForm.get('jobType')?.touched"
                >
                  <option value="">Select job type</option>
                  <option value="Delivery">Delivery</option>
                  <option value="Pickup">Pickup</option>
                  <option value="Transfer">Transfer</option>
                  <option value="Moving">Moving</option>
                  <option value="Courier">Courier</option>
                </select>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">
                  Vehicle Type Required
                </label>
                <select
                  formControlName="vehicleTypeRequired"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                >
                  <option [ngValue]="null">Any vehicle</option>
                  <option value="Car">Car</option>
                  <option value="Van">Van</option>
                  <option value="Truck">Truck</option>
                  <option value="Motorcycle">Motorcycle</option>
                </select>
              </div>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Priority
              </label>
              <select
                formControlName="priority"
                class="w-full md:w-64 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              >
                <option [ngValue]="null">Normal</option>
                <option value="Low">Low</option>
                <option value="Normal">Normal</option>
                <option value="High">High</option>
                <option value="Urgent">Urgent</option>
              </select>
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
                placeholder="123 Main St, City, State ZIP"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                [class.border-red-500]="templateForm.get('pickupLocation')?.invalid && templateForm.get('pickupLocation')?.touched"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Delivery Location <span class="text-red-600">*</span>
              </label>
              <input
                type="text"
                formControlName="deliveryLocation"
                placeholder="456 Oak Ave, City, State ZIP"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                [class.border-red-500]="templateForm.get('deliveryLocation')?.invalid && templateForm.get('deliveryLocation')?.touched"
              />
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">
                  Estimated Distance (miles)
                </label>
                <input
                  type="number"
                  formControlName="estimatedDistance"
                  step="0.1"
                  min="0"
                  placeholder="e.g., 5.2"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">
                  Estimated Duration (minutes)
                </label>
                <input
                  type="number"
                  formControlName="estimatedDuration"
                  min="0"
                  placeholder="e.g., 30"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
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
                placeholder="Any special handling or delivery instructions"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              ></textarea>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Customer Notes
              </label>
              <textarea
                formControlName="customerNotes"
                rows="3"
                placeholder="Private notes for your reference"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              ></textarea>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">
                Base Price (Optional)
              </label>
              <div class="relative">
                <span class="absolute left-3 top-2 text-gray-500">$</span>
                <input
                  type="number"
                  formControlName="basePrice"
                  step="0.01"
                  min="0"
                  placeholder="0.00"
                  class="w-full md:w-64 pl-7 pr-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
              <p class="mt-1 text-xs text-gray-500">Expected price for reference</p>
            </div>

            <div class="flex items-center gap-4">
              <label class="flex items-center">
                <input
                  type="checkbox"
                  formControlName="isDefault"
                  class="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                />
                <span class="ml-2 text-sm text-gray-700">Set as default template</span>
              </label>
            </div>
          </div>
        </div>

        <!-- Form Actions -->
        <div class="flex items-center justify-end gap-3 pt-4">
          <button
            type="button"
            routerLink="/customer/job-templates"
            class="px-6 py-2 text-gray-700 hover:text-gray-900 font-medium"
          >
            Cancel
          </button>
          <button
            type="submit"
            [disabled]="templateForm.invalid || isSaving"
            class="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ isSaving ? 'Saving...' : (isEditMode ? 'Update Template' : 'Create Template') }}
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
export class JobTemplateFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private jobTemplatesService = inject(JobTemplatesService);

  templateForm!: FormGroup;
  isEditMode = false;
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  templateId: string | null = null;

  ngOnInit(): void {
    this.initForm();
    this.checkEditMode();
  }

  initForm(): void {
    this.templateForm = this.fb.group({
      templateName: ['', Validators.required],
      description: [''],
      jobType: ['', Validators.required],
      vehicleTypeRequired: [null],
      priority: [null],
      pickupLocation: ['', Validators.required],
      pickupLatitude: [null],
      pickupLongitude: [null],
      deliveryLocation: ['', Validators.required],
      deliveryLatitude: [null],
      deliveryLongitude: [null],
      estimatedDistance: [null],
      estimatedDuration: [null],
      specialInstructions: [''],
      customerNotes: [''],
      basePrice: [null],
      isDefault: [false]
    });
  }

  checkEditMode(): void {
    this.templateId = this.route.snapshot.paramMap.get('id');
    if (this.templateId) {
      this.isEditMode = true;
      this.loadTemplate();
    }
  }

  loadTemplate(): void {
    if (!this.templateId) return;

    this.isLoading = true;
    this.errorMessage = '';

    this.jobTemplatesService.apiJobTemplatesIdGet(this.templateId).subscribe({
      next: (template: JobTemplate) => {
        this.templateForm.patchValue({
          templateName: template.templateName,
          description: template.description,
          jobType: template.jobType,
          vehicleTypeRequired: template.vehicleTypeRequired,
          priority: template.priority,
          pickupLocation: template.pickupLocation,
          pickupLatitude: template.pickupLatitude,
          pickupLongitude: template.pickupLongitude,
          deliveryLocation: template.deliveryLocation,
          deliveryLatitude: template.deliveryLatitude,
          deliveryLongitude: template.deliveryLongitude,
          estimatedDistance: template.estimatedDistance,
          estimatedDuration: template.estimatedDuration,
          specialInstructions: template.specialInstructions,
          customerNotes: template.customerNotes,
          basePrice: template.basePrice,
          isDefault: template.isDefault
        });
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading template:', error);
        this.errorMessage = 'Failed to load template. Please try again.';
        this.isLoading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.templateForm.invalid) {
      this.templateForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const formValue = this.templateForm.value;

    if (this.isEditMode && this.templateId) {
      const updateDto: UpdateJobTemplateDto = {
        templateName: formValue.templateName,
        description: formValue.description || null,
        jobType: formValue.jobType,
        vehicleTypeRequired: formValue.vehicleTypeRequired,
        priority: formValue.priority,
        pickupLocation: formValue.pickupLocation,
        deliveryLocation: formValue.deliveryLocation,
        estimatedDistance: formValue.estimatedDistance,
        estimatedDuration: formValue.estimatedDuration,
        specialInstructions: formValue.specialInstructions || null,
        customerNotes: formValue.customerNotes || null,
        basePrice: formValue.basePrice,
        isDefault: formValue.isDefault
      };

      this.jobTemplatesService.apiJobTemplatesIdPut(this.templateId, updateDto).subscribe({
        next: () => {
          this.router.navigate(['/customer/job-templates']);
        },
        error: (error: any) => {
          console.error('Error updating template:', error);
          this.errorMessage = 'Failed to update template. Please try again.';
          this.isSaving = false;
        }
      });
    } else {
      const createDto: CreateJobTemplateDto = {
        templateName: formValue.templateName,
        description: formValue.description || null,
        jobType: formValue.jobType,
        vehicleTypeRequired: formValue.vehicleTypeRequired,
        priority: formValue.priority,
        pickupLocation: formValue.pickupLocation,
        pickupLatitude: formValue.pickupLatitude,
        pickupLongitude: formValue.pickupLongitude,
        deliveryLocation: formValue.deliveryLocation,
        deliveryLatitude: formValue.deliveryLatitude,
        deliveryLongitude: formValue.deliveryLongitude,
        estimatedDistance: formValue.estimatedDistance,
        estimatedDuration: formValue.estimatedDuration,
        items: null,
        specialInstructions: formValue.specialInstructions || null,
        customerNotes: formValue.customerNotes || null,
        stops: null,
        basePrice: formValue.basePrice,
        tags: null,
        isDefault: formValue.isDefault
      };

      this.jobTemplatesService.apiJobTemplatesPost(createDto).subscribe({
        next: () => {
          this.router.navigate(['/customer/job-templates']);
        },
        error: (error: any) => {
          console.error('Error creating template:', error);
          this.errorMessage = 'Failed to create template. Please try again.';
          this.isSaving = false;
        }
      });
    }
  }
}
