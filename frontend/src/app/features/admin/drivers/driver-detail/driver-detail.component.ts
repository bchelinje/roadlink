import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { DriversService, DriverDto, JobDto, JobsService } from '@core/api';
import { HeaderComponent } from '@app/layout/header/header.component';

@Component({
  selector: 'app-driver-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent],
  templateUrl: './driver-detail.component.html',
  styleUrls: ['./driver-detail.component.scss']
})
export class DriverDetailComponent implements OnInit {
  private driversService = inject(DriversService);
  private jobsService = inject(JobsService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  driver: DriverDto | null = null;
  recentJobs: JobDto[] = [];
  isLoading = false;
  isLoadingJobs = false;
  errorMessage = '';

  driverId: string = '';

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.driverId = params['id'];
      if (this.driverId) {
        this.loadDriver();
        this.loadRecentJobs();
      }
    });
  }

  loadDriver(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.driversService.apiDriversIdGet(this.driverId).subscribe({
      next: (driver) => {
        this.driver = driver;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading driver:', error);
        this.errorMessage = 'Failed to load driver details. Please try again.';
        this.isLoading = false;
      }
    });
  }

  loadRecentJobs(): void {
    this.isLoadingJobs = true;

    // Load recent jobs for this driver using the Jobs API with driverId filter
    this.jobsService.apiJobsGet(undefined, undefined, undefined, this.driverId, undefined, undefined, undefined, undefined, undefined, 1, 5).subscribe({
      next: (result) => {
        this.recentJobs = result.items || [];
        this.isLoadingJobs = false;
      },
      error: (error) => {
        console.error('Error loading jobs:', error);
        this.isLoadingJobs = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/drivers']);
  }

  editDriver(): void {
    this.router.navigate(['/drivers', this.driverId, 'edit']);
  }

  getFullName(driver: DriverDto): string {
    return `${driver.firstName || ''} ${driver.lastName || ''}`.trim() || 'N/A';
  }

  getStatusBadgeClass(status: string | null): string {
    const statusMap: { [key: string]: string } = {
      'Available': 'bg-green-100 text-green-800',
      'OnJob': 'bg-blue-100 text-blue-800',
      'Offline': 'bg-gray-100 text-gray-800',
      'Inactive': 'bg-red-100 text-red-800'
    };
    return status ? statusMap[status] || 'bg-gray-100 text-gray-800' : 'bg-gray-100 text-gray-800';
  }

  getRatingStars(rating: number | undefined): string {
    if (!rating) return '☆☆☆☆☆';
    const fullStars = Math.floor(rating);
    const halfStar = rating % 1 >= 0.5 ? '½' : '';
    const emptyStars = 5 - Math.ceil(rating);
    return '★'.repeat(fullStars) + halfStar + '☆'.repeat(emptyStars);
  }

  isLicenseExpiring(driver: DriverDto): boolean {
    if (!driver.licenseExpiry) return false;
    const expiryDate = new Date(driver.licenseExpiry);
    const thirtyDaysFromNow = new Date();
    thirtyDaysFromNow.setDate(thirtyDaysFromNow.getDate() + 30);
    const now = new Date();
    return expiryDate <= thirtyDaysFromNow && expiryDate > now;
  }

  isLicenseExpired(driver: DriverDto): boolean {
    if (!driver.licenseExpiry) return false;
    const expiryDate = new Date(driver.licenseExpiry);
    return expiryDate <= new Date();
  }

  getJobStatusClass(status: string | null): string {
    const statusMap: { [key: string]: string } = {
      'Pending': 'bg-yellow-100 text-yellow-800',
      'Assigned': 'bg-blue-100 text-blue-800',
      'InProgress': 'bg-indigo-100 text-indigo-800',
      'Completed': 'bg-green-100 text-green-800',
      'Cancelled': 'bg-red-100 text-red-800'
    };
    return status ? statusMap[status] || 'bg-gray-100 text-gray-800' : 'bg-gray-100 text-gray-800';
  }

  getCompletionRate(): number {
    if (!this.driver || !this.driver.totalJobs || this.driver.totalJobs === 0) return 0;
    return Math.round(((this.driver.completedJobs || 0) / this.driver.totalJobs) * 100);
  }
}
