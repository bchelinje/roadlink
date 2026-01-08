import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  VehiclesService,
  VehicleViewModel,
  MaintenanceHistoryViewModel,
  LogMaintenanceModel
} from '@core/api';

@Component({
  selector: 'app-vehicle-maintenance',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="mb-6 flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Vehicle Maintenance</h1>
          <p *ngIf="vehicle" class="mt-1 text-sm text-gray-600">
            {{ vehicle.make }} {{ vehicle.model }} ({{ vehicle.registrationNumber }})
          </p>
        </div>
        <button
          (click)="goBack()"
          class="px-4 py-2 text-gray-700 hover:text-gray-900 font-medium"
        >
          ← Back to Vehicles
        </button>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading maintenance records...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage && !isLoading" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Success Message -->
      <div *ngIf="successMessage" class="mb-6 bg-green-50 border border-green-200 rounded-lg p-4">
        <p class="text-green-800">{{ successMessage }}</p>
      </div>

      <div *ngIf="!isLoading && vehicle">
        <!-- Vehicle Info Card -->
        <div class="bg-white border border-gray-200 rounded-lg p-6 mb-6">
          <h3 class="text-lg font-semibold text-gray-900 mb-4">Vehicle Information</h3>
          <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div>
              <p class="text-xs text-gray-500 mb-1">Registration Number</p>
              <p class="font-medium text-gray-900">{{ vehicle.registrationNumber }}</p>
            </div>
            <div>
              <p class="text-xs text-gray-500 mb-1">Year</p>
              <p class="font-medium text-gray-900">{{ vehicle.year }}</p>
            </div>
            <div>
              <p class="text-xs text-gray-500 mb-1">Current Mileage</p>
              <p class="font-medium text-gray-900">{{ vehicle.mileage?.toLocaleString() || 'N/A' }} mi</p>
            </div>
            <div>
              <p class="text-xs text-gray-500 mb-1">Status</p>
              <span
                [class]="getStatusClass(vehicle.status)"
                class="px-2 py-0.5 text-xs font-medium rounded"
              >
                {{ vehicle.status }}
              </span>
            </div>
            <div *ngIf="vehicle.lastInspectionDate">
              <p class="text-xs text-gray-500 mb-1">Last Inspection</p>
              <p class="font-medium text-gray-900">{{ formatDate(vehicle.lastInspectionDate) }}</p>
            </div>
            <div *ngIf="vehicle.nextInspectionDue">
              <p class="text-xs text-gray-500 mb-1">Next Inspection Due</p>
              <p
                class="font-medium"
                [class.text-red-600]="isInspectionOverdue(vehicle.nextInspectionDue)"
                [class.text-orange-600]="isInspectionDueSoon(vehicle.nextInspectionDue)"
                [class.text-gray-900]="!isInspectionOverdue(vehicle.nextInspectionDue) && !isInspectionDueSoon(vehicle.nextInspectionDue)"
              >
                {{ formatDate(vehicle.nextInspectionDue) }}
                <span *ngIf="isInspectionOverdue(vehicle.nextInspectionDue)">(OVERDUE)</span>
                <span *ngIf="isInspectionDueSoon(vehicle.nextInspectionDue) && !isInspectionOverdue(vehicle.nextInspectionDue)">(Soon)</span>
              </p>
            </div>
          </div>
        </div>

        <!-- Log New Maintenance -->
        <div class="bg-white border border-gray-200 rounded-lg p-6 mb-6">
          <h3 class="text-lg font-semibold text-gray-900 mb-4">Log New Maintenance</h3>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Maintenance Date</label>
              <input
                type="date"
                [(ngModel)]="newMaintenance.maintenanceDate"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Next Inspection Due</label>
              <input
                type="date"
                [(ngModel)]="newMaintenance.nextInspectionDue"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Mileage at Service</label>
              <input
                type="number"
                [(ngModel)]="newMaintenance.mileage"
                min="0"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg"
              />
            </div>

            <div class="md:col-span-2">
              <label class="block text-sm font-medium text-gray-700 mb-1">Description *</label>
              <textarea
                [(ngModel)]="newMaintenance.description"
                rows="3"
                placeholder="Enter maintenance details (oil change, tire rotation, brake service, etc.)"
                class="w-full px-3 py-2 border border-gray-300 rounded-lg"
              ></textarea>
            </div>
          </div>

          <button
            (click)="logMaintenance()"
            [disabled]="isSubmitting || !newMaintenance.description"
            class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium text-sm disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ isSubmitting ? 'Logging...' : 'Log Maintenance' }}
          </button>
        </div>

        <!-- Maintenance History -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h3 class="text-lg font-semibold text-gray-900 mb-4">Maintenance History ({{ maintenanceHistory.length }})</h3>

          <div *ngIf="maintenanceHistory.length === 0" class="text-center py-12">
            <svg class="w-24 h-24 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
            </svg>
            <h3 class="mt-4 text-lg font-medium text-gray-900">No maintenance records</h3>
            <p class="mt-2 text-gray-600">Start logging maintenance activities to track vehicle service history</p>
          </div>

          <div *ngIf="maintenanceHistory.length > 0" class="space-y-4">
            <div
              *ngFor="let record of maintenanceHistory"
              class="border border-gray-200 rounded-lg p-4 hover:border-blue-300 transition-colors"
            >
              <div class="flex items-start justify-between mb-2">
                <div class="flex-1">
                  <div class="flex items-center gap-3 mb-1">
                    <span class="text-sm font-medium text-gray-900">
                      {{ formatDateTime(record.timestamp) }}
                    </span>
                    <span *ngIf="record.userName" class="text-xs text-gray-500">
                      by {{ record.userName }}
                    </span>
                  </div>
                  <p class="text-gray-700">{{ record.description }}</p>
                </div>
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
export class VehicleMaintenanceComponent implements OnInit {
  private vehiclesService = inject(VehiclesService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  vehicleId = '';
  vehicle: VehicleViewModel | null = null;
  maintenanceHistory: MaintenanceHistoryViewModel[] = [];

  newMaintenance: LogMaintenanceModel = {
    maintenanceDate: new Date().toISOString().split('T')[0],
    nextInspectionDue: null,
    mileage: null,
    description: null
  };

  isLoading = false;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.vehicleId = this.route.snapshot.paramMap.get('id') || '';
    if (this.vehicleId) {
      this.loadData();
    }
  }

  loadData(): void {
    this.loadVehicle();
    this.loadMaintenanceHistory();
  }

  loadVehicle(): void {
    this.vehiclesService.apiVehiclesIdGet(this.vehicleId).subscribe({
      next: (vehicle: VehicleViewModel) => {
        this.vehicle = vehicle;
        if (vehicle.mileage) {
          this.newMaintenance.mileage = vehicle.mileage;
        }
      },
      error: (error: any) => {
        console.error('Error loading vehicle:', error);
        this.errorMessage = 'Failed to load vehicle information.';
      }
    });
  }

  loadMaintenanceHistory(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.vehiclesService.apiVehiclesIdMaintenanceHistoryGet(this.vehicleId).subscribe({
      next: (history: MaintenanceHistoryViewModel[]) => {
        this.maintenanceHistory = history || [];
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading maintenance history:', error);
        this.errorMessage = 'Failed to load maintenance history.';
        this.isLoading = false;
      }
    });
  }

  logMaintenance(): void {
    if (!this.newMaintenance.description) {
      this.errorMessage = 'Please enter a maintenance description.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.vehiclesService.apiVehiclesIdMaintenancePost(this.vehicleId, this.newMaintenance).subscribe({
      next: () => {
        this.successMessage = 'Maintenance logged successfully!';
        this.newMaintenance = {
          maintenanceDate: new Date().toISOString().split('T')[0],
          nextInspectionDue: null,
          mileage: this.vehicle?.mileage || null,
          description: null
        };
        this.loadData();
        this.isSubmitting = false;
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error: any) => {
        console.error('Error logging maintenance:', error);
        this.errorMessage = 'Failed to log maintenance. Please try again.';
        this.isSubmitting = false;
      }
    });
  }

  isInspectionOverdue(dueDate: string | null | undefined): boolean {
    if (!dueDate) return false;
    const due = new Date(dueDate);
    const today = new Date();
    return due < today;
  }

  isInspectionDueSoon(dueDate: string | null | undefined): boolean {
    if (!dueDate) return false;
    const due = new Date(dueDate);
    const today = new Date();
    const daysUntilDue = Math.ceil((due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    return daysUntilDue > 0 && daysUntilDue <= 30;
  }

  getStatusClass(status: string | null | undefined): string {
    switch (status) {
      case 'Active':
        return 'bg-green-100 text-green-800';
      case 'Inactive':
        return 'bg-gray-100 text-gray-800';
      case 'Maintenance':
        return 'bg-orange-100 text-orange-800';
      case 'OutOfService':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
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
    this.router.navigate(['/drivers']);
  }
}
