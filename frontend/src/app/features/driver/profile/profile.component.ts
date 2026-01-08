// features/driver/profile/profile.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DriversService } from '@core/api/api/drivers.service';
import { DriverDto, UpdateDriverDto } from '@core/api/model/models';
import { DriverReviewsComponent } from '../../../shared/components/driver-reviews/driver-reviews.component';

@Component({
  selector: 'app-driver-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, DriverReviewsComponent],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class DriverProfileComponent implements OnInit {
  private readonly driversService = inject(DriversService);

  driver: DriverDto | null = null;
  loading = true;
  error: string | null = null;
  saving = false;
  saveError: string | null = null;
  saveSuccess = false;

  // Edit mode
  editMode = false;
  editForm: Partial<UpdateDriverDto> = {};

  // Status options
  statusOptions = [
    { value: 'available', label: 'Available', color: 'green' },
    { value: 'on_job', label: 'On Job', color: 'blue' },
    { value: 'unavailable', label: 'Unavailable', color: 'yellow' },
    { value: 'inactive', label: 'Inactive', color: 'gray' },
    { value: 'suspended', label: 'Suspended', color: 'red' }
  ];

  ngOnInit(): void {
    this.loadDriverProfile();
  }

  private loadDriverProfile(): void {
    this.loading = true;
    this.error = null;

    this.driversService.apiDriversMeGet().subscribe({
      next: (driver: DriverDto) => {
        this.driver = driver;
        this.initializeEditForm();
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Failed to load driver profile:', err);
        this.error = 'Failed to load profile. Please try again.';
        this.loading = false;
      }
    });
  }

  private initializeEditForm(): void {
    if (!this.driver) return;

    this.editForm = {
      phone: this.driver.phone,
      licenseNumber: this.driver.licenseNumber,
      licenseExpiry: this.driver.licenseExpiry,
      vehicleType: this.driver.vehicleType,
      vehicleRegistration: this.driver.vehicleRegistration,
      address: this.driver.address,
      emergencyContact: this.driver.emergencyContact
    };
  }

  toggleEditMode(): void {
    if (this.editMode) {
      // Cancel edit - reset form
      this.initializeEditForm();
    }
    this.editMode = !this.editMode;
    this.saveError = null;
    this.saveSuccess = false;
  }

  updateStatus(newStatus: string): void {
    if (!this.driver?.id) return;

    this.saving = true;
    this.saveError = null;

    this.driversService.apiDriversMeStatusPatch({ status: newStatus }).subscribe({
      next: () => {
        this.saving = false;
        this.saveSuccess = true;
        this.loadDriverProfile();
        setTimeout(() => this.saveSuccess = false, 3000);
      },
      error: (err: any) => {
        console.error('Failed to update status:', err);
        this.saveError = 'Failed to update status. Please try again.';
        this.saving = false;
      }
    });
  }

  saveProfile(): void {
    if (!this.driver?.id) return;

    this.saving = true;
    this.saveError = null;
    this.saveSuccess = false;

    this.driversService.apiDriversIdPut(this.driver.id, this.editForm as UpdateDriverDto).subscribe({
      next: () => {
        this.saving = false;
        this.saveSuccess = true;
        this.editMode = false;
        this.loadDriverProfile();
        setTimeout(() => this.saveSuccess = false, 3000);
      },
      error: (err: any) => {
        console.error('Failed to save profile:', err);
        this.saveError = 'Failed to save profile. Please try again.';
        this.saving = false;
      }
    });
  }

  uploadProfileImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || !input.files[0] || !this.driver?.id) return;

    const file = input.files[0];

    this.driversService.apiDriversIdProfileImagePost(this.driver.id, file).subscribe({
      next: () => {
        this.loadDriverProfile();
      },
      error: (err: any) => {
        console.error('Failed to upload profile image:', err);
        alert('Failed to upload profile image. Please try again.');
      }
    });
  }

  getStatusBadgeColor(status: string): string {
    const option = this.statusOptions.find(s => s.value === status);
    return option?.color || 'gray';
  }

  getStatusBadgeClass(status: string): string {
    const color = this.getStatusBadgeColor(status);
    const colorMap: Record<string, string> = {
      'green': 'bg-green-100 text-green-800',
      'blue': 'bg-blue-100 text-blue-800',
      'yellow': 'bg-yellow-100 text-yellow-800',
      'gray': 'bg-gray-100 text-gray-800',
      'red': 'bg-red-100 text-red-800'
    };
    return colorMap[color] || 'bg-gray-100 text-gray-800';
  }

  formatStatus(status: string): string {
    return status.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  formatDate(date: string | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      month: 'long',
      day: 'numeric',
      year: 'numeric'
    });
  }

  getRatingStars(rating: number): number[] {
    return Array(5).fill(0).map((_, i) => i < Math.floor(rating) ? 1 : 0);
  }
}
