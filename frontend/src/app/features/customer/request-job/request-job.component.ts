import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { JobsService, CreateJobDto } from '@core/api';

@Component({
  selector: 'app-request-job',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './request-job.component.html',
  styleUrls: ['./request-job.component.scss']
})
export class RequestJobComponent {
  private fb = inject(FormBuilder);
  private jobsService = inject(JobsService);
  private router = inject(Router);

  jobForm: FormGroup;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';

  jobTypes = [
    { value: 'local_move', label: 'Local Move' },
    { value: 'long_distance', label: 'Long Distance Move' },
    { value: 'commercial', label: 'Commercial Delivery' },
    { value: 'furniture', label: 'Furniture Transport' },
    { value: 'appliance', label: 'Appliance Delivery' },
    { value: 'other', label: 'Other' }
  ];

  vehicleTypes = [
    { value: 'van', label: 'Van' },
    { value: 'cargo_van', label: 'Cargo Van' },
    { value: 'small_truck', label: 'Small Truck (10-14 ft)' },
    { value: 'large_truck', label: 'Large Truck (16-26 ft)' },
    { value: 'box_truck', label: 'Box Truck' }
  ];

  priorities = [
    { value: 'low', label: 'Flexible - No Rush' },
    { value: 'normal', label: 'Normal Priority' },
    { value: 'high', label: 'High Priority' },
    { value: 'urgent', label: 'Urgent - ASAP' }
  ];

  constructor() {
    const today = new Date().toISOString().split('T')[0];

    this.jobForm = this.fb.group({
      // Customer Information
      customerName: ['', [Validators.required, Validators.minLength(2)]],
      customerEmail: ['', [Validators.required, Validators.email]],
      customerPhone: ['', [Validators.required, Validators.pattern(/^\+?[\d\s\-()]+$/)]],

      // Job Details
      jobType: ['local_move', Validators.required],
      vehicleTypeRequired: ['van'],
      priority: ['normal', Validators.required],
      scheduledDate: [today, Validators.required],
      scheduledTime: ['09:00', Validators.required],
      estimatedDuration: [120, [Validators.required, Validators.min(30)]],

      // Locations
      pickupAddress: ['', Validators.required],
      pickupCity: ['', Validators.required],
      pickupState: ['', Validators.required],
      pickupZip: ['', Validators.required],

      deliveryAddress: ['', Validators.required],
      deliveryCity: ['', Validators.required],
      deliveryState: ['', Validators.required],
      deliveryZip: ['', Validators.required],

      // Items
      itemDescription: ['', [Validators.required, Validators.minLength(10)]],

      // Additional Info
      specialInstructions: [''],
      customerNotes: ['']
    });
  }

  onSubmit(): void {
    if (this.jobForm.invalid) {
      this.markFormGroupTouched(this.jobForm);
      this.errorMessage = 'Please fill in all required fields correctly.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const formValue = this.jobForm.value;

    // Build location objects
    const pickupLocation = {
      address: formValue.pickupAddress,
      city: formValue.pickupCity,
      state: formValue.pickupState,
      zipCode: formValue.pickupZip
    };

    const deliveryLocation = {
      address: formValue.deliveryAddress,
      city: formValue.deliveryCity,
      state: formValue.deliveryState,
      zipCode: formValue.deliveryZip
    };

    // Build items array
    const items = [{
      description: formValue.itemDescription,
      quantity: 1
    }];

    const jobDto: CreateJobDto = {
      customerName: formValue.customerName,
      customerEmail: formValue.customerEmail,
      customerPhone: formValue.customerPhone,
      jobType: formValue.jobType,
      vehicleTypeRequired: formValue.vehicleTypeRequired || undefined,
      priority: formValue.priority,
      scheduledDate: formValue.scheduledDate,
      scheduledTime: formValue.scheduledTime,
      estimatedDuration: formValue.estimatedDuration,
      pickupLocation: JSON.stringify(pickupLocation),
      deliveryLocation: JSON.stringify(deliveryLocation),
      items: JSON.stringify(items),
      specialInstructions: formValue.specialInstructions || undefined,
      internalNotes: formValue.customerNotes || undefined // Store customer notes as internal notes
    };

    this.jobsService.apiJobsPost(jobDto).subscribe({
      next: (response: any) => {
        const jobNumber = response.jobNumber || 'Pending';
        this.successMessage = `Your job request has been submitted successfully! Job Number: ${jobNumber}. We'll contact you shortly at ${formValue.customerEmail}.`;
        this.jobForm.reset();
        this.isSubmitting = false;

        // Scroll to success message
        window.scrollTo({ top: 0, behavior: 'smooth' });
      },
      error: (error) => {
        console.error('Error submitting job request:', error);
        this.errorMessage = error.error?.title || 'Failed to submit job request. Please try again or contact us directly.';
        this.isSubmitting = false;
      }
    });
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.jobForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.jobForm.get(fieldName);
    if (field?.hasError('required')) return 'This field is required';
    if (field?.hasError('email')) return 'Please enter a valid email';
    if (field?.hasError('pattern')) return 'Please enter a valid phone number';
    if (field?.hasError('minLength')) return 'Please provide more details';
    if (field?.hasError('min')) return 'Minimum duration is 30 minutes';
    return '';
  }
}
