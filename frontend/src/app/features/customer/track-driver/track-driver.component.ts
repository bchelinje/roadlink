import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { LocationService, DriverLocationViewModel } from '@core/api';
import { interval, Subscription } from 'rxjs';
import { switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-track-driver',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="mb-6">
        <h1 class="text-2xl font-bold text-gray-900">Track Your Driver</h1>
        <p class="mt-1 text-sm text-gray-600">Real-time location and ETA for your job</p>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading driver location...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage && !isLoading" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Driver Location -->
      <div *ngIf="!isLoading && driverLocation" class="space-y-6">
        <!-- ETA Card -->
        <div class="bg-gradient-to-r from-blue-500 to-blue-600 rounded-lg p-6 text-white">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm opacity-90">Estimated Time of Arrival</p>
              <p class="text-4xl font-bold mt-2">{{ eta?.estimatedMinutes || '--' }} min</p>
              <p class="text-sm mt-1 opacity-90" *ngIf="eta?.estimatedArrival">
                Arriving at {{ formatTime(eta.estimatedArrival) }}
              </p>
            </div>
            <svg class="w-20 h-20 opacity-80" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
        </div>

        <!-- Driver Info Card -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Driver Information</h2>

          <div class="space-y-4">
            <div class="flex items-center gap-3">
              <div class="w-12 h-12 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white font-semibold">
                {{ getInitials(driverLocation.driverName) }}
              </div>
              <div>
                <p class="font-medium text-gray-900">{{ driverLocation.driverName || 'Driver' }}</p>
                <p class="text-sm text-gray-600">On the way to you</p>
              </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4 pt-4 border-t border-gray-200">
              <div>
                <p class="text-xs text-gray-500 mb-1">Current Speed</p>
                <p class="font-medium text-gray-900">
                  {{ driverLocation.speed ? (driverLocation.speed * 2.237).toFixed(0) : '--' }} mph
                </p>
              </div>

              <div>
                <p class="text-xs text-gray-500 mb-1">Last Updated</p>
                <p class="font-medium text-gray-900">{{ formatTimestamp(driverLocation.timestamp) }}</p>
              </div>

              <div class="md:col-span-2" *ngIf="driverLocation.address">
                <p class="text-xs text-gray-500 mb-1">Current Location</p>
                <p class="text-sm text-gray-900">{{ driverLocation.address }}</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Map Placeholder -->
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">Live Map</h2>

          <div class="bg-gray-100 rounded-lg flex items-center justify-center h-96">
            <div class="text-center">
              <svg class="w-16 h-16 mx-auto text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
              </svg>
              <p class="text-gray-600 font-medium">Map View</p>
              <p class="text-sm text-gray-500 mt-1">
                Latitude: {{ driverLocation.latitude?.toFixed(6) }}<br>
                Longitude: {{ driverLocation.longitude?.toFixed(6) }}
              </p>
              <p class="text-xs text-gray-400 mt-3">
                Map integration coming soon
              </p>
            </div>
          </div>
        </div>

        <!-- Auto-refresh indicator -->
        <div class="flex items-center justify-center gap-2 text-sm text-gray-600">
          <div class="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
          <span>Auto-refreshing every 10 seconds</span>
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
export class TrackDriverComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private locationService = inject(LocationService);

  driverLocation: DriverLocationViewModel | null = null;
  eta: any = null;
  isLoading = false;
  errorMessage = '';
  jobId: string | null = null;

  private refreshSubscription?: Subscription;

  ngOnInit(): void {
    this.jobId = this.route.snapshot.paramMap.get('jobId');
    if (this.jobId) {
      this.loadDriverLocation();
      this.loadEta();
      this.startAutoRefresh();
    } else {
      this.errorMessage = 'Job ID not found';
    }
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  loadDriverLocation(): void {
    if (!this.jobId) return;

    this.isLoading = true;
    this.errorMessage = '';

    this.locationService.apiLocationJobsJobIdDriverLocationGet(this.jobId).subscribe({
      next: (location: DriverLocationViewModel) => {
        this.driverLocation = location;
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading driver location:', error);
        this.errorMessage = 'Unable to load driver location. The driver may not have started the job yet.';
        this.isLoading = false;
      }
    });
  }

  loadEta(): void {
    if (!this.jobId) return;

    this.locationService.apiLocationJobsJobIdEtaGet(this.jobId).subscribe({
      next: (eta: any) => {
        this.eta = eta;
      },
      error: (error: any) => {
        console.error('Error loading ETA:', error);
      }
    });
  }

  startAutoRefresh(): void {
    // Refresh every 10 seconds
    this.refreshSubscription = interval(10000)
      .pipe(
        switchMap(() => {
          if (this.jobId) {
            return this.locationService.apiLocationJobsJobIdDriverLocationGet(this.jobId);
          }
          return [];
        })
      )
      .subscribe({
        next: (location: DriverLocationViewModel) => {
          this.driverLocation = location;
          this.loadEta(); // Also refresh ETA
        },
        error: (error: any) => {
          console.error('Auto-refresh error:', error);
        }
      });
  }

  stopAutoRefresh(): void {
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
    }
  }

  getInitials(name: string | null | undefined): string {
    if (!name) return 'D';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 1).toUpperCase();
  }

  formatTimestamp(timestamp: string | undefined): string {
    if (!timestamp) return 'Unknown';
    const date = new Date(timestamp);
    const now = new Date();
    const diffSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    if (diffSeconds < 30) return 'Just now';
    if (diffSeconds < 60) return `${diffSeconds} seconds ago`;
    if (diffSeconds < 120) return '1 minute ago';
    if (diffSeconds < 3600) return `${Math.floor(diffSeconds / 60)} minutes ago`;
    return date.toLocaleTimeString();
  }

  formatTime(dateString: string | undefined): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
  }
}
