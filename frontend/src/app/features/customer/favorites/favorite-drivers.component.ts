import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CustomersService, Driver } from '@core/api';

@Component({
  selector: 'app-favorite-drivers',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container mx-auto px-4 py-8">
      <div class="page-header">
        <div>
          <h1>Favorite Drivers</h1>
          <p class="subtitle">Drivers you've marked as favorites for future bookings</p>
        </div>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="text-center py-12">
        <div class="inline-block w-12 h-12 border-4 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-600">Loading favorites...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage && !isLoading" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
        <p class="text-red-800">{{ errorMessage }}</p>
      </div>

      <!-- Empty State -->
      <div *ngIf="!isLoading && favorites.length === 0 && !errorMessage" class="text-center py-12">
        <svg class="w-24 h-24 mx-auto text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
        </svg>
        <h3 class="mt-4 text-lg font-medium text-gray-900">No favorite drivers yet</h3>
        <p class="mt-2 text-gray-600">Mark drivers as favorites after completing jobs with them</p>
        <button
          routerLink="/customer/my-jobs"
          class="mt-4 px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors"
        >
          View My Jobs
        </button>
      </div>

      <!-- Favorites Grid -->
      <div *ngIf="!isLoading && favorites.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <div
          *ngFor="let driver of favorites"
          class="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-lg transition-shadow"
        >
          <!-- Driver Header -->
          <div class="flex items-center gap-4 mb-4">
            <div class="w-16 h-16 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white font-semibold text-xl">
              {{ getInitials(driver.firstName, driver.lastName) }}
            </div>
            <div class="flex-1">
              <h3 class="text-lg font-semibold text-gray-900">{{ driver.firstName }} {{ driver.lastName }}</h3>
              <div class="flex items-center gap-1 mt-1">
                <svg class="w-4 h-4 text-yellow-400 fill-current" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                </svg>
                <span class="text-sm font-medium text-gray-900">{{ driver.rating?.toFixed(1) || 'N/A' }}</span>
                <span class="text-sm text-gray-500">({{ driver.completedJobs || 0 }} jobs)</span>
              </div>
            </div>
          </div>

          <!-- Driver Contact (if available) -->
          <div *ngIf="driver.phone || driver.email" class="space-y-2 mb-4 text-sm text-gray-600">
            <div *ngIf="driver.phone" class="flex items-center gap-2">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
              </svg>
              <span>{{ driver.phone }}</span>
            </div>
            <div *ngIf="driver.email" class="flex items-center gap-2">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              </svg>
              <span class="truncate">{{ driver.email }}</span>
            </div>
          </div>

          <!-- Actions -->
          <div class="pt-4 border-t border-gray-200">
            <button
              routerLink="/customer/request-job"
              [queryParams]="{ driverId: driver.id }"
              class="w-full px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium text-sm transition-colors"
            >
              Request Job
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      padding: 2rem;
      background: linear-gradient(135deg, #003d82 0%, #001f3f 100%);
      border-radius: 1rem;
      color: white;
    }
    .page-header h1 {
      margin: 0 0 0.5rem 0;
      font-size: 2rem;
      font-weight: 700;
    }
    .page-header .subtitle {
      margin: 0;
      opacity: 0.9;
      font-size: 1rem;
    }
    @media (max-width: 768px) {
      .page-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 1.5rem;
      }
    }
  `]
})
export class FavoriteDriversComponent implements OnInit {
  private customersService = inject(CustomersService);

  favorites: Driver[] = [];
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadFavorites();
  }

  loadFavorites(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.customersService.apiCustomersMeFavoritesGet().subscribe({
      next: (favorites: Driver[]) => {
        this.favorites = favorites || [];
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading favorites:', error);
        this.errorMessage = 'Failed to load favorite drivers. Please try again.';
        this.isLoading = false;
      }
    });
  }

  getInitials(firstName: string | null | undefined, lastName: string | null | undefined): string {
    if (!firstName && !lastName) return '?';
    if (firstName && lastName) {
      return (firstName[0] + lastName[0]).toUpperCase();
    }
    if (firstName) {
      return firstName.substring(0, 2).toUpperCase();
    }
    if (lastName) {
      return lastName.substring(0, 2).toUpperCase();
    }
    return '?';
  }
}
