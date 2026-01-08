import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DriversService, UsersService, DriverDto, CreateDriverDto, UpdateDriverDto, UserViewModel } from '@core/api';
import { HeaderComponent } from '@app/layout/header/header.component';

@Component({
  selector: 'app-driver-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, HeaderComponent],
  templateUrl: './driver-form.component.html',
  styleUrls: ['./driver-form.component.scss']
})
export class DriverFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private driversService = inject(DriversService);
  private usersService = inject(UsersService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  driverForm: FormGroup;
  driver: DriverDto | null = null;
  driverId: string | null = null;
  isEditMode = false;
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  successMessage = '';

  users: UserViewModel[] = [];
  isLoadingUsers = false;

  statuses = ['Available', 'OnJob', 'Offline', 'Inactive'];
  vehicleTypes = ['Van', 'Truck', 'Pickup', 'Box Truck', 'Flatbed'];

  constructor() {
    this.driverForm = this.fb.group({
      userId: ['', Validators.required],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', Validators.required],
      licenseNumber: ['', Validators.required],
      licenseExpiry: ['', Validators.required],
      vehicleType: [''],
      vehicleRegistration: [''],
      status: ['Available'],
      address: [''],
      emergencyContact: ['']
    });
  }

  ngOnInit(): void {
    this.driverId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.driverId;

    if (this.isEditMode) {
      this.loadDriver();
      // Disable userId field in edit mode
      this.driverForm.get('userId')?.disable();
    } else {
      this.loadUsers();
    }
  }

  loadDriver(): void {
    if (!this.driverId) return;

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

  loadUsers(): void {
    this.isLoadingUsers = true;

    // Load all users - in production you might want pagination
    this.usersService.apiUsersGet(1, 1000).subscribe({
      next: (response) => {
        this.users = response.users || [];
        this.isLoadingUsers = false;
      },
      error: (error) => {
        console.error('Error loading users:', error);
        this.errorMessage = 'Failed to load users. Please try again.';
        this.isLoadingUsers = false;
      }
    });
  }

  populateForm(driver: DriverDto): void {
    this.driverForm.patchValue({
      userId: driver.userId,
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
  }

  formatDateForInput(dateString: string): string {
    const date = new Date(dateString);
    return date.toISOString().split('T')[0];
  }

  onUserChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const userId = select.value;
    const user = this.users.find(u => u.id === userId);

    if (user) {
      // Auto-populate form fields from selected user
      // Parse userName to try to extract first/last name
      const nameParts = (user.userName || '').split(' ');
      const firstName = nameParts[0] || '';
      const lastName = nameParts.slice(1).join(' ') || '';

      this.driverForm.patchValue({
        firstName: firstName,
        lastName: lastName,
        email: user.email || '',
        phone: user.phoneNumber || ''
      });
    }
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

    if (this.isEditMode) {
      this.updateDriver();
    } else {
      this.createDriver();
    }
  }

  createDriver(): void {
    const formValue = this.driverForm.value;

    const createDto: CreateDriverDto = {
      userId: formValue.userId,
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      email: formValue.email,
      phone: formValue.phone,
      licenseNumber: formValue.licenseNumber,
      licenseExpiry: formValue.licenseExpiry,
      vehicleType: formValue.vehicleType || undefined,
      vehicleRegistration: formValue.vehicleRegistration || undefined,
      status: formValue.status || 'Available',
      address: formValue.address || undefined,
      emergencyContact: formValue.emergencyContact || undefined
    };

    this.driversService.apiDriversPost(createDto).subscribe({
      next: (driver) => {
        this.successMessage = 'Driver created successfully!';
        this.isSaving = false;
        setTimeout(() => {
          this.router.navigate(['/drivers', driver.id]);
        }, 1500);
      },
      error: (error) => {
        console.error('Error creating driver:', error);
        this.errorMessage = error.error?.message || error.error || 'Failed to create driver. Please try again.';
        this.isSaving = false;
      }
    });
  }

  updateDriver(): void {
    if (!this.driverId) return;

    const formValue = this.driverForm.value;

    const updateDto: UpdateDriverDto = {
      phone: formValue.phone,
      licenseNumber: formValue.licenseNumber || undefined,
      licenseExpiry: formValue.licenseExpiry || undefined,
      vehicleType: formValue.vehicleType || undefined,
      vehicleRegistration: formValue.vehicleRegistration || undefined,
      address: formValue.address || undefined,
      emergencyContact: formValue.emergencyContact || undefined
    };

    this.driversService.apiDriversIdPut(this.driverId, updateDto).subscribe({
      next: () => {
        this.successMessage = 'Driver updated successfully!';
        this.isSaving = false;
        setTimeout(() => {
          this.router.navigate(['/drivers', this.driverId]);
        }, 1500);
      },
      error: (error) => {
        console.error('Error updating driver:', error);
        this.errorMessage = error.error?.message || 'Failed to update driver. Please try again.';
        this.isSaving = false;
      }
    });
  }

  cancel(): void {
    if (this.isEditMode && this.driverId) {
      this.router.navigate(['/drivers', this.driverId]);
    } else {
      this.router.navigate(['/drivers']);
    }
  }

  get f() {
    return this.driverForm.controls;
  }
}
