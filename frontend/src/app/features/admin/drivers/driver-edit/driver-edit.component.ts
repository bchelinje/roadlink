import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DriversService, DriverDto, UpdateDriverDto } from '@core/api';
import { HeaderComponent } from '@app/layout/header/header.component';

@Component({
  selector: 'app-driver-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, HeaderComponent],
  templateUrl: './driver-edit.component.html',
  styleUrls: ['./driver-edit.component.scss']
})
export class DriverEditComponent implements OnInit {
  private fb = inject(FormBuilder);
  private driversService = inject(DriversService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  driverForm: FormGroup;
  driver: DriverDto | null = null;
  driverId: string = '';
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  successMessage = '';

  profileImageFile: File | null = null;
  profileImagePreview: string | null = null;

  statuses = ['Available', 'OnJob', 'Offline', 'Inactive'];

  constructor() {
    this.driverForm = this.fb.group({
      // These fields are read-only (cannot be updated via API)
      firstName: [{value: '', disabled: true}],
      lastName: [{value: '', disabled: true}],
      email: [{value: '', disabled: true}],
      status: [{value: 'Available', disabled: true}],
      // These fields are editable
      phone: ['', [Validators.required]],
      licenseNumber: [''],
      licenseExpiry: [''],
      vehicleType: [''],
      vehicleRegistration: [''],
      address: [''],
      emergencyContact: ['']
    });
  }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.driverId = params['id'];
      if (this.driverId) {
        this.loadDriver();
      }
    });
  }

  loadDriver(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.driversService.apiDriversIdGet(this.driverId).subscribe({
      next: (driver) => {
        this.driver = driver;
        this.populateForm(driver);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading driver:', error);
        this.errorMessage = 'Failed to load driver details. Please try again.';
        this.isLoading = false;
      }
    });
  }

  populateForm(driver: DriverDto): void {
    this.driverForm.patchValue({
      firstName: driver.firstName,
      lastName: driver.lastName,
      email: driver.email,
      phone: driver.phone,
      licenseNumber: driver.licenseNumber,
      licenseExpiry: driver.licenseExpiry ? this.formatDateForInput(driver.licenseExpiry) : '',
      vehicleType: driver.vehicleType,
      vehicleRegistration: driver.vehicleRegistration,
      status: driver.status || 'Available',
      address: driver.address,
      emergencyContact: driver.emergencyContact
    });

    if (driver.profileImage) {
      this.profileImagePreview = driver.profileImage;
    }
  }

  formatDateForInput(dateString: string): string {
    const date = new Date(dateString);
    return date.toISOString().split('T')[0];
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];

      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.errorMessage = 'Please select a valid image file.';
        return;
      }

      // Validate file size (max 5MB)
      if (file.size > 5 * 1024 * 1024) {
        this.errorMessage = 'Image size must be less than 5MB.';
        return;
      }

      this.profileImageFile = file;

      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        this.profileImagePreview = e.target?.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  removeImage(): void {
    this.profileImageFile = null;
    this.profileImagePreview = null;
  }

  onSubmit(): void {
    if (this.driverForm.invalid) {
      Object.keys(this.driverForm.controls).forEach(key => {
        this.driverForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.successMessage = '';

    const formValue = this.driverForm.value;
    // UpdateDriverDto only allows updating certain fields
    // firstName, lastName, and email are not updatable via this DTO
    const updateDto: UpdateDriverDto = {
      phone: formValue.phone,
      licenseNumber: formValue.licenseNumber || null,
      licenseExpiry: formValue.licenseExpiry || null,
      vehicleType: formValue.vehicleType || null,
      vehicleRegistration: formValue.vehicleRegistration || null,
      address: formValue.address || null,
      emergencyContact: formValue.emergencyContact || null
    };

    this.driversService.apiDriversIdPut(this.driverId, updateDto).subscribe({
      next: () => {
        // If there's a profile image, upload it
        if (this.profileImageFile) {
          this.uploadProfileImage();
        } else {
          this.successMessage = 'Driver updated successfully!';
          this.isSaving = false;
          setTimeout(() => {
            this.router.navigate(['/drivers', this.driverId]);
          }, 1500);
        }
      },
      error: (error) => {
        console.error('Error updating driver:', error);
        this.errorMessage = error.error?.message || 'Failed to update driver. Please try again.';
        this.isSaving = false;
      }
    });
  }

  uploadProfileImage(): void {
    if (!this.profileImageFile) return;

    this.driversService.apiDriversIdProfileImagePost(this.driverId, this.profileImageFile).subscribe({
      next: () => {
        this.successMessage = 'Driver and profile image updated successfully!';
        this.isSaving = false;
        setTimeout(() => {
          this.router.navigate(['/drivers', this.driverId]);
        }, 1500);
      },
      error: (error) => {
        console.error('Error uploading profile image:', error);
        this.errorMessage = 'Driver updated but failed to upload profile image.';
        this.isSaving = false;
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/drivers', this.driverId]);
  }

  get f() {
    return this.driverForm.controls;
  }
}
