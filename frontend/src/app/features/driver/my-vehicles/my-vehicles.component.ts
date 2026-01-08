// features/driver/my-vehicles/my-vehicles.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DriversService } from '@core/api/api/drivers.service';
import { ToastService } from '@core/services/toast.service';
import { VehicleDto } from '@core/api/model/vehicleDto';

@Component({
  selector: 'app-my-vehicles',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './my-vehicles.component.html',
  styleUrls: ['./my-vehicles.component.scss']
})
export class MyVehiclesComponent implements OnInit {
  private readonly driversService = inject(DriversService);
  private readonly toastService = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  vehicles: VehicleDto[] = [];
  selectedVehicle: VehicleDto | null = null;
  loading = true;
  error: string | null = null;
  showAddForm = false;
  showEditForm = false;
  isSaving = false;

  vehicleForm: FormGroup;

  vehicleTypes = ['Van', 'Cargo Van', 'Box Truck', 'Flatbed', 'Pickup', 'Small Truck', 'Large Truck'];
  vehicleStatuses = ['active', 'inactive', 'maintenance', 'retired'];

  constructor() {
    this.vehicleForm = this.fb.group({
      type: ['', Validators.required],
      make: ['', Validators.required],
      model: ['', Validators.required],
      year: ['', [Validators.required, Validators.min(1900), Validators.max(new Date().getFullYear() + 1)]],
      registrationNumber: ['', Validators.required],
      vinNumber: [''],
      cargoCapacity: [''],
      maxPayloadWeight: [''],
      maxGrossWeight: [''],
      cargoLength: [''],
      cargoWidth: [''],
      cargoHeight: [''],
      features: [''],
      hasInsurance: [false],
      insuranceExpiry: [''],
      status: ['active'],
      lastInspectionDate: [''],
      nextInspectionDue: [''],
      mileage: [''],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    this.loadVehicles();
  }

  private loadVehicles(): void {
    this.loading = true;
    this.error = null;

    this.driversService.apiDriversMeVehiclesGet().subscribe({
      next: (vehicles: VehicleDto[]) => {
        this.vehicles = vehicles || [];
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Failed to load vehicles:', err);
        this.error = 'Failed to load vehicles. Please try again.';
        this.toastService.error('Error', 'Failed to load vehicles. Please try again.');
        this.loading = false;
      }
    });
  }

  openAddForm(): void {
    this.showAddForm = true;
    this.showEditForm = false;
    this.selectedVehicle = null;
    this.vehicleForm.reset({
      status: 'active',
      hasInsurance: false,
      isActive: true
    });
  }

  openEditForm(vehicle: VehicleDto): void {
    this.showEditForm = true;
    this.showAddForm = false;
    this.selectedVehicle = vehicle;

    this.vehicleForm.patchValue({
      type: vehicle.type,
      make: vehicle.make,
      model: vehicle.model,
      year: vehicle.year,
      registrationNumber: vehicle.registrationNumber,
      vinNumber: vehicle.vinNumber,
      cargoCapacity: vehicle.cargoCapacity,
      maxPayloadWeight: vehicle.maxPayloadWeight,
      maxGrossWeight: vehicle.maxGrossWeight,
      cargoLength: vehicle.cargoLength,
      cargoWidth: vehicle.cargoWidth,
      cargoHeight: vehicle.cargoHeight,
      features: vehicle.features,
      hasInsurance: vehicle.hasInsurance,
      insuranceExpiry: vehicle.insuranceExpiry ? this.formatDateForInput(vehicle.insuranceExpiry) : '',
      status: vehicle.status,
      lastInspectionDate: vehicle.lastInspectionDate ? this.formatDateForInput(vehicle.lastInspectionDate) : '',
      nextInspectionDue: vehicle.nextInspectionDue ? this.formatDateForInput(vehicle.nextInspectionDue) : '',
      mileage: vehicle.mileage,
      isActive: vehicle.isActive
    });
  }

  closeForm(): void {
    this.showAddForm = false;
    this.showEditForm = false;
    this.selectedVehicle = null;
    this.vehicleForm.reset();
  }

  onSubmit(): void {
    if (this.vehicleForm.invalid) {
      Object.keys(this.vehicleForm.controls).forEach(key => {
        this.vehicleForm.get(key)?.markAsTouched();
      });
      return;
    }

    if (this.showEditForm && this.selectedVehicle) {
      this.updateVehicle();
    } else {
      this.addVehicle();
    }
  }

  private addVehicle(): void {
    this.isSaving = true;
    this.error = null;

    const vehicleData = this.vehicleForm.value;

    this.driversService.apiDriversMeVehiclesPost(vehicleData).subscribe({
      next: (vehicle: VehicleDto) => {
        this.vehicles.push(vehicle);
        this.toastService.success('Success', 'Vehicle added successfully');
        this.closeForm();
        this.isSaving = false;
      },
      error: (err: any) => {
        console.error('Failed to add vehicle:', err);
        const errorMessage = err.error?.message || 'Failed to add vehicle. Please try again.';
        this.error = errorMessage;
        this.toastService.error('Error', errorMessage);
        this.isSaving = false;
      }
    });
  }

  private updateVehicle(): void {
    if (!this.selectedVehicle?.id) return;

    this.isSaving = true;
    this.error = null;

    const vehicleData = this.vehicleForm.value;

    this.driversService.apiDriversMeVehiclesIdPut(this.selectedVehicle.id, vehicleData).subscribe({
      next: (vehicle: VehicleDto) => {
        const index = this.vehicles.findIndex(v => v.id === vehicle.id);
        if (index !== -1) {
          this.vehicles[index] = vehicle;
        }
        this.toastService.success('Success', 'Vehicle updated successfully');
        this.closeForm();
        this.isSaving = false;
      },
      error: (err: any) => {
        console.error('Failed to update vehicle:', err);
        const errorMessage = err.error?.message || 'Failed to update vehicle. Please try again.';
        this.error = errorMessage;
        this.toastService.error('Error', errorMessage);
        this.isSaving = false;
      }
    });
  }

  deleteVehicle(vehicle: VehicleDto): void {
    if (!confirm(`Are you sure you want to delete ${vehicle.year} ${vehicle.make} ${vehicle.model}?`)) {
      return;
    }

    if (!vehicle.id) return;

    this.driversService.apiDriversMeVehiclesIdDelete(vehicle.id).subscribe({
      next: () => {
        this.vehicles = this.vehicles.filter(v => v.id !== vehicle.id);
        this.toastService.success('Success', 'Vehicle deleted successfully');
      },
      error: (err: any) => {
        console.error('Failed to delete vehicle:', err);
        const errorMessage = err.error?.message || 'Failed to delete vehicle. Please try again.';
        this.error = errorMessage;
        this.toastService.error('Error', errorMessage);
      }
    });
  }

  toggleVehicleStatus(vehicle: VehicleDto): void {
    if (!vehicle.id) return;

    const newStatus = vehicle.status === 'active' ? 'inactive' : 'active';

    // Note: The DriversService doesn't have a status patch endpoint, so we need to update the whole vehicle
    const updateData = {
      ...vehicle,
      status: newStatus
    };

    this.driversService.apiDriversMeVehiclesIdPut(vehicle.id, updateData).subscribe({
      next: (updatedVehicle: VehicleDto) => {
        const index = this.vehicles.findIndex(v => v.id === vehicle.id);
        if (index !== -1) {
          this.vehicles[index] = updatedVehicle;
        }
        this.toastService.success('Success', `Vehicle status changed to ${newStatus}`);
      },
      error: (err: any) => {
        console.error('Failed to update vehicle status:', err);
        const errorMessage = err.error?.message || 'Failed to update vehicle status. Please try again.';
        this.error = errorMessage;
        this.toastService.error('Error', errorMessage);
      }
    });
  }

  getStatusBadgeClass(status: string | null | undefined): string {
    if (!status) return 'bg-gray-100 text-gray-800';
    const statusMap: Record<string, string> = {
      'active': 'bg-green-100 text-green-800',
      'inactive': 'bg-gray-100 text-gray-800',
      'maintenance': 'bg-yellow-100 text-yellow-800',
      'retired': 'bg-red-100 text-red-800'
    };
    return statusMap[status] || 'bg-gray-100 text-gray-800';
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  formatDateForInput(dateString: string): string {
    const date = new Date(dateString);
    return date.toISOString().split('T')[0];
  }

  isInsuranceExpiring(vehicle: VehicleDto): boolean {
    if (!vehicle.insuranceExpiry) return false;
    const expiryDate = new Date(vehicle.insuranceExpiry);
    const thirtyDaysFromNow = new Date();
    thirtyDaysFromNow.setDate(thirtyDaysFromNow.getDate() + 30);
    const now = new Date();
    return expiryDate <= thirtyDaysFromNow && expiryDate > now;
  }

  isInsuranceExpired(vehicle: VehicleDto): boolean {
    if (!vehicle.insuranceExpiry) return false;
    const expiryDate = new Date(vehicle.insuranceExpiry);
    return expiryDate <= new Date();
  }

  isInspectionDue(vehicle: VehicleDto): boolean {
    if (!vehicle.nextInspectionDue) return false;
    const dueDate = new Date(vehicle.nextInspectionDue);
    return dueDate <= new Date();
  }

  get f() {
    return this.vehicleForm.controls;
  }
}
