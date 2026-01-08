import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocationService, DriverLocationViewModel } from '@core/api';
import { interval, Subscription } from 'rxjs';
import { switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-active-drivers',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen bg-gray-50 py-8">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">

        <div class="page-header">
          <div>
            <h1>Active Drivers</h1>
            <p class="subtitle">Real-time locations of all active drivers</p>
          </div>
          <div class="flex items-center gap-2 text-sm">
            <div class="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
            <span class="text-white">Live updates</span>
          </div>
        </div>
        
<!--        <div class="mb-6 flex items-center justify-between">-->
<!--          <div>-->
<!--            <h1 class="text-2xl font-bold text-gray-900">Active Drivers</h1>-->
<!--            <p class="mt-1 text-sm text-gray-600">Real-time locations of all active drivers</p>-->
<!--          </div>-->

<!--        <div class="flex items-center gap-2 text-sm">-->
<!--          <div class="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>-->
<!--          <span class="text-gray-600">Live updates</span>-->
<!--        </div>-->
<!--      </div>-->

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading active drivers...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage && !isLoading" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Stats -->
      <div *ngIf="!isLoading && activeDrivers.length > 0" class="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <p class="text-sm text-gray-600 mb-1">Active Drivers</p>
          <p class="text-3xl font-bold text-gray-900">{{ activeDrivers.length }}</p>
        </div>

        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <p class="text-sm text-gray-600 mb-1">Moving</p>
          <p class="text-3xl font-bold text-green-600">{{ countMovingDrivers() }}</p>
        </div>

        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <p class="text-sm text-gray-600 mb-1">Stationary</p>
          <p class="text-3xl font-bold text-orange-600">{{ countStationaryDrivers() }}</p>
        </div>

        <div class="bg-white border border-gray-200 rounded-lg p-6">
          <p class="text-sm text-gray-600 mb-1">Avg Speed</p>
          <p class="text-3xl font-bold text-blue-600">{{ calculateAverageSpeed().toFixed(0) }} mph</p>
        </div>
      </div>

      <!-- Empty State -->
      <div *ngIf="!isLoading && activeDrivers.length === 0 && !errorMessage" class="text-center py-12">
        <svg class="w-24 h-24 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
        </svg>
        <h3 class="mt-4 text-lg font-medium text-gray-900">No active drivers</h3>
        <p class="mt-2 text-gray-600">No drivers are currently active with location tracking</p>
      </div>

      <!-- Drivers Grid -->
      <div *ngIf="!isLoading && activeDrivers.length > 0" class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div
          *ngFor="let driver of activeDrivers"
          class="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-lg transition-shadow"
        >
          <!-- Driver Header -->
          <div class="flex items-start justify-between mb-4">
            <div class="flex items-center gap-3">
              <div class="w-12 h-12 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white font-semibold">
                {{ getInitials(driver.driverName) }}
              </div>
              <div>
                <p class="font-semibold text-gray-900">{{ driver.driverName || 'Unknown Driver' }}</p>
                <p class="text-xs text-gray-500">ID: {{ driver.driverId }}</p>
              </div>
            </div>

            <div class="flex items-center gap-1">
              <div
                [class]="isMoving(driver) ? 'bg-green-500' : 'bg-orange-500'"
                class="w-2 h-2 rounded-full"
              ></div>
              <span class="text-xs font-medium" [class]="isMoving(driver) ? 'text-green-700' : 'text-orange-700'">
                {{ isMoving(driver) ? 'Moving' : 'Stationary' }}
              </span>
            </div>
          </div>

          <!-- Location Details -->
          <div class="space-y-3">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <p class="text-xs text-gray-500 mb-1">Speed</p>
                <p class="font-medium text-gray-900">
                  {{ driver.speed ? (driver.speed * 2.237).toFixed(0) : '0' }} mph
                </p>
              </div>

              <div>
                <p class="text-xs text-gray-500 mb-1">Heading</p>
                <p class="font-medium text-gray-900">{{ getHeadingDirection(driver.heading) }}</p>
              </div>
            </div>

            <div>
              <p class="text-xs text-gray-500 mb-1">Coordinates</p>
              <p class="text-sm font-mono text-gray-700">
                {{ driver.latitude?.toFixed(6) }}, {{ driver.longitude?.toFixed(6) }}
              </p>
            </div>

            <div *ngIf="driver.address">
              <p class="text-xs text-gray-500 mb-1">Current Location</p>
              <p class="text-sm text-gray-900">{{ driver.address }}</p>
            </div>

            <div class="pt-3 border-t border-gray-200">
              <div class="flex items-center justify-between text-xs">
                <span class="text-gray-500">Last Updated:</span>
                <span class="font-medium text-gray-900">{{ formatTimestamp(driver.timestamp) }}</span>
              </div>
              <div class="flex items-center justify-between text-xs mt-1">
                <span class="text-gray-500">Age:</span>
                <span
                  [class]="driver.ageInSeconds && driver.ageInSeconds > 60 ? 'text-orange-600' : 'text-green-600'"
                  class="font-medium"
                >
                  {{ driver.ageInSeconds || 0 }}s ago
                </span>
              </div>
            </div>
          </div>

          <!-- Map Placeholder -->
          <div class="mt-4 bg-gray-100 rounded-lg flex items-center justify-center h-32">
            <div class="text-center">
              <svg class="w-8 h-8 mx-auto text-gray-400 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
              </svg>
              <p class="text-xs text-gray-500">Mini map view</p>
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
export class ActiveDriversComponent implements OnInit, OnDestroy {
  private locationService = inject(LocationService);

  activeDrivers: DriverLocationViewModel[] = [];
  isLoading = false;
  errorMessage = '';

  private refreshSubscription?: Subscription;

  ngOnInit(): void {
    this.loadActiveDrivers();
    this.startAutoRefresh();
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  loadActiveDrivers(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.locationService.apiLocationDriversActiveGet().subscribe({
      next: (drivers: DriverLocationViewModel[]) => {
        this.activeDrivers = drivers || [];
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading active drivers:', error);
        this.errorMessage = 'Failed to load active drivers. Please try again.';
        this.isLoading = false;
      }
    });
  }

  startAutoRefresh(): void {
    // Refresh every 15 seconds
    this.refreshSubscription = interval(15000)
      .pipe(
        switchMap(() => this.locationService.apiLocationDriversActiveGet())
      )
      .subscribe({
        next: (drivers: DriverLocationViewModel[]) => {
          this.activeDrivers = drivers || [];
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

  isMoving(driver: DriverLocationViewModel): boolean {
    return (driver.speed || 0) > 0.5; // Moving if speed > 0.5 m/s (~1 mph)
  }

  countMovingDrivers(): number {
    return this.activeDrivers.filter(d => this.isMoving(d)).length;
  }

  countStationaryDrivers(): number {
    return this.activeDrivers.filter(d => !this.isMoving(d)).length;
  }

  calculateAverageSpeed(): number {
    if (this.activeDrivers.length === 0) return 0;
    const totalSpeed = this.activeDrivers.reduce((sum, d) => sum + ((d.speed || 0) * 2.237), 0);
    return totalSpeed / this.activeDrivers.length;
  }

  getHeadingDirection(heading: number | null | undefined): string {
    if (heading === null || heading === undefined) return 'N/A';

    const directions = ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW'];
    const index = Math.round(heading / 45) % 8;
    return directions[index];
  }

  getInitials(name: string | null | undefined): string {
    if (!name) return '?';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 1).toUpperCase();
  }

  formatTimestamp(timestamp: string | undefined): string {
    if (!timestamp) return 'Unknown';
    const date = new Date(timestamp);
    return date.toLocaleTimeString();
  }
}
