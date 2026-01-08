import { Component, EventEmitter, Input, Output, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { JobsService, DriversService, DriverDto, AssignJobDto } from '@core/api';

@Component({
  selector: 'app-assign-driver-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './assign-driver-modal.component.html',
  styleUrls: ['./assign-driver-modal.component.scss']
})
export class AssignDriverModalComponent implements OnInit {
  private jobsService = inject(JobsService);
  private driversService = inject(DriversService);

  @Input() jobId!: string;
  @Input() isOpen = false;
  @Output() isOpenChange = new EventEmitter<boolean>();
  @Output() assigned = new EventEmitter<void>();

  drivers: DriverDto[] = [];
  filteredDrivers: DriverDto[] = [];
  selectedDriverId: string | null = null;
  searchTerm = '';
  internalNotes = '';
  isLoading = false;
  isAssigning = false;
  errorMessage = '';

  ngOnInit(): void {
    if (this.isOpen) {
      this.loadDrivers();
    }
  }

  ngOnChanges(): void {
    if (this.isOpen) {
      this.loadDrivers();
    }
  }

  loadDrivers(): void {
    this.isLoading = true;
    this.errorMessage = '';

    // Load all available drivers for assignment
    // Note: Database stores lowercase status values: 'available', 'on_job', 'unavailable', 'inactive'
    this.driversService.apiDriversGet('available', undefined, undefined, undefined, undefined, undefined, 1, 100).subscribe({
      next: (response: any) => {
        this.drivers = response.items || [];
        this.filteredDrivers = [...this.drivers];
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading drivers:', error);
        this.errorMessage = 'Failed to load drivers. Please try again.';
        this.isLoading = false;
      }
    });
  }

  onSearchChange(): void {
    const search = this.searchTerm.toLowerCase();
    this.filteredDrivers = this.drivers.filter(driver =>
      driver.firstName?.toLowerCase().includes(search) ||
      driver.lastName?.toLowerCase().includes(search) ||
      driver.email?.toLowerCase().includes(search) ||
      driver.phone?.toLowerCase().includes(search)
    );
  }

  selectDriver(driverId: string): void {
    this.selectedDriverId = driverId;
  }

  assignDriver(): void {
    if (!this.selectedDriverId) {
      this.errorMessage = 'Please select a driver to assign.';
      return;
    }

    this.isAssigning = true;
    this.errorMessage = '';

    const assignDto: AssignJobDto = {
      driverId: this.selectedDriverId,
      internalNotes: this.internalNotes || undefined
    };

    this.jobsService.apiJobsIdAssignPost(this.jobId, assignDto).subscribe({
      next: () => {
        this.isAssigning = false;
        this.assigned.emit();
        this.close();
      },
      error: (error) => {
        console.error('Error assigning driver:', error);
        this.errorMessage = error.error?.title || 'Failed to assign driver. Please try again.';
        this.isAssigning = false;
      }
    });
  }

  close(): void {
    this.isOpen = false;
    this.isOpenChange.emit(false);
    this.selectedDriverId = null;
    this.searchTerm = '';
    this.internalNotes = '';
    this.errorMessage = '';
  }

  getDriverFullName(driver: DriverDto): string {
    return `${driver.firstName || ''} ${driver.lastName || ''}`.trim() || 'N/A';
  }

  getDriverVehicle(driver: DriverDto): string {
    return driver.vehicleType || 'Not specified';
  }
}
