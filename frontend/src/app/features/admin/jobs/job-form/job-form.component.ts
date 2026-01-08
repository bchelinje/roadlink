import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { JobsService, DriversService, CreateJobDto, UpdateJobDto, JobDto, DriverDto } from '@core/api';
import { HeaderComponent } from '@app/layout/header/header.component';

@Component({
  selector: 'app-job-form',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, HeaderComponent],
  templateUrl: './job-form.component.html',
  styleUrls: ['./job-form.component.scss']
})
export class JobFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private jobsService = inject(JobsService);
  private driversService = inject(DriversService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  jobForm: FormGroup;
  availableDrivers: DriverDto[] = [];
  isLoading = false;
  isEditMode = false;
  jobId: string | null = null;
  errorMessage = '';
  successMessage = '';

  jobTypes = ['local_move', 'long_distance', 'commercial', 'residential', 'office_move', 'furniture_delivery'];
  vehicleTypes = ['van', 'cargo_van', 'small_truck', 'large_truck', 'box_truck', 'minivan', 'pickup_truck'];
  priorities = ['low', 'normal', 'high', 'urgent'];

  constructor() {
    this.jobForm = this.fb.group({
      customerName: ['', [Validators.required, Validators.maxLength(255)]],
      customerPhone: ['', [Validators.required, Validators.pattern(/^\+?[1-9]\d{1,14}$/)]],
      customerEmail: ['', [Validators.required, Validators.email]],
      jobType: ['local_move', Validators.required],
      vehicleTypeRequired: [''],
      priority: ['normal', Validators.required],
      scheduledDate: ['', Validators.required],
      scheduledTime: ['', Validators.required],
      estimatedDuration: [60, [Validators.min(15)]],
      pickupLocation: ['', Validators.required],
      deliveryLocation: ['', Validators.required],
      distance: [0],
      items: ['', Validators.required],
      specialInstructions: [''],
      internalNotes: [''],
      driverId: ['']
    });
  }

  ngOnInit(): void {
    this.jobId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.jobId;

    if (this.isEditMode) {
      this.loadJob();
    }

    this.loadDrivers();
  }

  loadJob(): void {
    if (!this.jobId) return;

    this.isLoading = true;
    this.jobsService.apiJobsIdGet(this.jobId).subscribe({
      next: (job: JobDto) => {
        this.populateForm(job);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading job:', error);
        this.errorMessage = 'Failed to load job details.';
        this.isLoading = false;
      }
    });
  }

  populateForm(job: JobDto): void {
    // Format date for input
    const scheduledDate = job.scheduledDate ? new Date(job.scheduledDate).toISOString().split('T')[0] : '';

    this.jobForm.patchValue({
      customerName: job.customerName,
      customerPhone: job.customerPhone,
      customerEmail: job.customerEmail,
      jobType: job.jobType,
      vehicleTypeRequired: job.vehicleTypeRequired || '',
      priority: job.priority,
      scheduledDate: scheduledDate,
      scheduledTime: job.scheduledTime,
      estimatedDuration: job.estimatedDuration || 60,
      pickupLocation: job.pickupLocation,
      deliveryLocation: job.deliveryLocation,
      distance: job.distance || 0,
      items: job.items,
      specialInstructions: job.specialInstructions || '',
      internalNotes: job.internalNotes || '',
      driverId: job.driverId || ''
    });
  }

  loadDrivers(): void {
    // Note: You might need to implement a get all drivers endpoint
    // For now, this is a placeholder
    this.availableDrivers = [];
  }

  onSubmit(): void {
    if (this.jobForm.invalid) {
      this.markFormGroupTouched(this.jobForm);
      this.errorMessage = 'Please fill in all required fields correctly.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    if (this.isEditMode) {
      this.updateJob();
    } else {
      this.createJob();
    }
  }

  createJob(): void {
    const formValue = this.jobForm.value;

    const createDto: CreateJobDto = {
      customerName: formValue.customerName,
      customerPhone: formValue.customerPhone,
      customerEmail: formValue.customerEmail,
      jobType: formValue.jobType,
      vehicleTypeRequired: formValue.vehicleTypeRequired || undefined,
      priority: formValue.priority,
      scheduledDate: formValue.scheduledDate,
      scheduledTime: formValue.scheduledTime,
      estimatedDuration: formValue.estimatedDuration || 60,
      pickupLocation: formValue.pickupLocation,
      deliveryLocation: formValue.deliveryLocation,
      distance: formValue.distance || undefined,
      items: formValue.items,
      specialInstructions: formValue.specialInstructions || undefined,
      internalNotes: formValue.internalNotes || undefined,
      driverId: formValue.driverId || undefined
    };

    this.jobsService.apiJobsPost(createDto).subscribe({
      next: (job) => {
        this.successMessage = 'Job created successfully!';
        this.isLoading = false;
        setTimeout(() => {
          this.router.navigate(['/jobs', job.id]);
        }, 1500);
      },
      error: (error) => {
        console.error('Error creating job:', error);
        this.errorMessage = error.error?.title || 'Failed to create job. Please try again.';
        this.isLoading = false;
      }
    });
  }

  updateJob(): void {
    if (!this.jobId) return;

    const formValue = this.jobForm.value;

    const updateDto: UpdateJobDto = {
      customerName: formValue.customerName,
      customerPhone: formValue.customerPhone,
      customerEmail: formValue.customerEmail,
      jobType: formValue.jobType,
      vehicleTypeRequired: formValue.vehicleTypeRequired || undefined,
      priority: formValue.priority,
      scheduledDate: formValue.scheduledDate,
      scheduledTime: formValue.scheduledTime,
      estimatedDuration: formValue.estimatedDuration || 60,
      pickupLocation: formValue.pickupLocation,
      deliveryLocation: formValue.deliveryLocation,
      distance: formValue.distance || undefined,
      items: formValue.items,
      specialInstructions: formValue.specialInstructions || undefined,
      internalNotes: formValue.internalNotes || undefined
    };

    this.jobsService.apiJobsIdPut(this.jobId, updateDto).subscribe({
      next: (job) => {
        this.successMessage = 'Job updated successfully!';
        this.isLoading = false;
        setTimeout(() => {
          this.router.navigate(['/jobs', job.id]);
        }, 1500);
      },
      error: (error) => {
        console.error('Error updating job:', error);
        this.errorMessage = error.error?.title || 'Failed to update job. Please try again.';
        this.isLoading = false;
      }
    });
  }

  onCancel(): void {
    if (this.isEditMode && this.jobId) {
      this.router.navigate(['/jobs', this.jobId]);
    } else {
      this.router.navigate(['/jobs']);
    }
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.jobForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.jobForm.get(fieldName);
    if (!field || !field.errors) return '';

    if (field.errors['required']) return `${fieldName} is required`;
    if (field.errors['email']) return 'Invalid email format';
    if (field.errors['pattern']) return 'Invalid format';
    if (field.errors['maxLength']) return `Maximum length exceeded`;
    if (field.errors['min']) return `Value too low`;

    return 'Invalid value';
  }
}
