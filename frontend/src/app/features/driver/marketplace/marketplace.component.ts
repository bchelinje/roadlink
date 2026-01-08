// features/driver/marketplace/marketplace.component.ts
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DriversService } from '@core/api/api/drivers.service';
import { JobDto } from '@core/api/model/models';
import { PlaceBidModalComponent } from './components/place-bid-modal.component';

@Component({
  selector: 'app-marketplace',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, PlaceBidModalComponent],
  templateUrl: './marketplace.component.html',
  styleUrls: ['./marketplace.component.scss']
})
export class MarketplaceComponent implements OnInit {
  private readonly driversService = inject(DriversService);

  jobs: JobDto[] = [];
  filteredJobs: JobDto[] = [];
  loading = true;
  error: string | null = null;
  claimingJobId: string | null = null;

  // Bid modal state
  showBidModal = false;
  selectedJobForBid: JobDto | null = null;

  // Expose Math to template
  Math = Math;

  // Filters
  selectedVehicleType: string = 'all';
  selectedJobType: string = 'all';
  locationFilter: string = '';
  searchTerm: string = '';
  sortBy: string = 'scheduledDate';
  sortOrder: 'asc' | 'desc' = 'asc';

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalItems = 0;

  vehicleTypes = [
    { value: 'all', label: 'All Vehicle Types' },
    { value: 'van', label: 'Van' },
    { value: 'cargo_van', label: 'Cargo Van' },
    { value: 'small_truck', label: 'Small Truck' },
    { value: 'large_truck', label: 'Large Truck' },
    { value: 'box_truck', label: 'Box Truck' }
  ];

  jobTypes = [
    { value: 'all', label: 'All Job Types' },
    { value: 'local_move', label: 'Local Move' },
    { value: 'long_distance', label: 'Long Distance' },
    { value: 'commercial', label: 'Commercial' },
    { value: 'residential', label: 'Residential' }
  ];

  ngOnInit(): void {
    this.loadMarketplaceJobs();
  }

  private loadMarketplaceJobs(): void {
    this.loading = true;
    this.error = null;

    const location = this.locationFilter.trim() || undefined;
    const vehicleType = this.selectedVehicleType !== 'all' ? this.selectedVehicleType : undefined;
    const jobType = this.selectedJobType !== 'all' ? this.selectedJobType : undefined;

    // Call the marketplace endpoint
    this.driversService.apiDriversMarketplaceJobsGet(location, vehicleType, jobType, undefined, undefined, this.currentPage, this.pageSize).subscribe({
      next: (response: any) => {
        this.jobs = response.items || [];
        this.totalItems = response.total || 0;
        this.applyLocalFilters();
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Failed to load marketplace jobs:', err);
        this.error = 'Failed to load marketplace jobs. Please try again.';
        this.loading = false;
      }
    });
  }

  applyLocalFilters(): void {
    let filtered = [...this.jobs];

    // Apply search filter
    if (this.searchTerm.trim()) {
      const search = this.searchTerm.toLowerCase();
      filtered = filtered.filter(job =>
        job.jobNumber?.toLowerCase().includes(search) ||
        job.customerName?.toLowerCase().includes(search) ||
        job.pickupLocation?.toLowerCase().includes(search) ||
        job.deliveryLocation?.toLowerCase().includes(search)
      );
    }

    // Apply sorting
    filtered.sort((a, b) => {
      let aValue: any, bValue: any;

      switch (this.sortBy) {
        case 'scheduledDate':
          aValue = new Date(a.scheduledDate || 0).getTime();
          bValue = new Date(b.scheduledDate || 0).getTime();
          break;
        case 'jobNumber':
          aValue = a.jobNumber || '';
          bValue = b.jobNumber || '';
          break;
        case 'distance':
          aValue = a.distance || 0;
          bValue = b.distance || 0;
          break;
        default:
          return 0;
      }

      if (aValue < bValue) return this.sortOrder === 'asc' ? -1 : 1;
      if (aValue > bValue) return this.sortOrder === 'asc' ? 1 : -1;
      return 0;
    });

    this.filteredJobs = filtered;
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadMarketplaceJobs();
  }

  onLocalFilterChange(): void {
    this.applyLocalFilters();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadMarketplaceJobs();
  }

  clearFilters(): void {
    this.selectedVehicleType = 'all';
    this.selectedJobType = 'all';
    this.locationFilter = '';
    this.searchTerm = '';
    this.sortBy = 'scheduledDate';
    this.sortOrder = 'asc';
    this.currentPage = 1;
    this.loadMarketplaceJobs();
  }

  openBidModal(job: JobDto): void {
    this.selectedJobForBid = job;
    this.showBidModal = true;
  }

  closeBidModal(): void {
    this.showBidModal = false;
    this.selectedJobForBid = null;
  }

  onBidPlaced(): void {
    // Reload marketplace jobs to reflect the new bid
    this.loadMarketplaceJobs();
  }

  claimJob(jobId: string): void {
    if (this.claimingJobId) {
      return; // Already claiming a job
    }

    if (!confirm('Are you sure you want to claim this job?')) {
      return;
    }

    this.claimingJobId = jobId;
    this.error = null;

    this.driversService.apiDriversMarketplaceJobsJobIdClaimPost(jobId).subscribe({
      next: (response) => {
        console.log('Job claimed successfully:', response);
        // Remove the claimed job from the list
        this.jobs = this.jobs.filter(j => j.id !== jobId);
        this.applyLocalFilters();
        this.claimingJobId = null;
        alert('Job claimed successfully! You can view it in "My Jobs".');
      },
      error: (err: any) => {
        console.error('Failed to claim job:', err);
        this.error = err.error?.message || 'Failed to claim job. It may have been claimed by another driver.';
        this.claimingJobId = null;
      }
    });
  }

  getJobTypeBadgeClass(jobType: string): string {
    const typeMap: Record<string, string> = {
      'local_move': 'bg-blue-100 text-blue-800',
      'long_distance': 'bg-purple-100 text-purple-800',
      'commercial': 'bg-green-100 text-green-800',
      'residential': 'bg-yellow-100 text-yellow-800'
    };
    return typeMap[jobType] || 'bg-gray-100 text-gray-800';
  }

  getPriorityBadgeClass(priority: string): string {
    const priorityMap: Record<string, string> = {
      'urgent': 'bg-red-100 text-red-800',
      'high': 'bg-orange-100 text-orange-800',
      'normal': 'bg-blue-100 text-blue-800',
      'low': 'bg-gray-100 text-gray-800'
    };
    return priorityMap[priority] || 'bg-gray-100 text-gray-800';
  }

  formatJobType(jobType: string): string {
    return jobType.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  }

  formatPriority(priority: string): string {
    return priority.charAt(0).toUpperCase() + priority.slice(1);
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  formatTime(time: string | null | undefined): string {
    if (!time) return 'N/A';
    // If time is just a string like "09:00", format it appropriately
    if (time && time.includes(':')) {
      return time;
    }
    return new Date(time).toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  }

  parseLocation(locationJson: string | null | undefined): any {
    if (!locationJson) return null;
    try {
      return JSON.parse(locationJson);
    } catch {
      return { address: locationJson };
    }
  }

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize);
  }

  get pages(): number[] {
    const pages: number[] = [];
    for (let i = 1; i <= this.totalPages; i++) {
      pages.push(i);
    }
    return pages;
  }
}
